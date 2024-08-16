// Copyright 2023 TELUS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Concurrent;
using Buf.Validate;
using Cel;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProtoValidate.Exceptions;
using ProtoValidate.Internal.Cel;
using ProtoValidate.Internal.Constraints;

namespace ProtoValidate.Internal.Evaluator.Evaluator;

public class EvaluatorBuilder
{
    private static readonly string[] GoogleWellKnownTypes =
    {
        "google.protobuf.DoubleValue",
        "google.protobuf.FloatValue",
        "google.protobuf.Int64Value",
        "google.protobuf.UInt64Value",
        "google.protobuf.Int32Value",
        "google.protobuf.UInt32Value",
        "google.protobuf.BoolValue",
        "google.protobuf.StringValue",
        "google.protobuf.BytesValue",
        "google.protobuf.Timestamp",
        "google.protobuf.Duration",
        "google.protobuf.Any"
    };

    private readonly ConcurrentDictionary<MessageDescriptor, IEvaluator> EvaluatorMap = new();

    public EvaluatorBuilder(CelEnvironment celEnvironment, bool disableLazy)
    {
        DisableLazy = disableLazy;
        CelEnvironment = celEnvironment;
        Constraints = new ConstraintCache(celEnvironment);
    }

    private ConstraintResolver ConstraintResolver { get; } = new();
    private bool DisableLazy { get; }
    private CelEnvironment CelEnvironment { get; }
    private ConstraintCache Constraints { get; }

    /// <summary>
    ///     Returns a pre-cached Evaluator for the given descriptor or, if the descriptor is
    ///     unknown, returns an evaluator that always throws a CompilationException.
    /// </summary>
    /// <param name="messageDescriptor">The message descriptor of the message we want to validate.</param>
    /// <returns>The evaluator for the specified descriptor.</returns>
    public IEvaluator Load(MessageDescriptor messageDescriptor)
    {
        if (DisableLazy)
        {
            return LoadDescriptor(messageDescriptor);
        }

        return LoadOrBuildDescriptor(messageDescriptor);
    }

    private IEvaluator LoadDescriptor(MessageDescriptor messageDescriptor)
    {
        if (!EvaluatorMap.TryGetValue(messageDescriptor, out var evaluator))
        {
            return new UnknownDescriptorEvaluator(messageDescriptor);
        }

        return evaluator;
    }

    private IEvaluator LoadOrBuildDescriptor(MessageDescriptor messageDescriptor)
    {
        return EvaluatorMap.GetOrAdd(messageDescriptor, Build);
    }

    private IEvaluator Build(MessageDescriptor messageDescriptor)
    {
        return Build(messageDescriptor, new Dictionary<MessageDescriptor, IEvaluator>());
    }

    private IEvaluator Build(MessageDescriptor messageDescriptor, IDictionary<MessageDescriptor, IEvaluator> inProgressMessageEvaluators)
    {
        return EvaluatorMap.GetOrAdd(messageDescriptor, c_messageDescriptor =>
        {
            var messageEvaluator = new MessageEvaluator(c_messageDescriptor);

            //when we are building messages, they could be recursive on themselves
            //we use this dictionary to track that
            //so that the EvaluatorMap can remain thread-safe and won't get updated until the evaluators are fully built.
            //doing this keeps those nested evaluators on the stack (thread-safe) until we need them.
            inProgressMessageEvaluators[c_messageDescriptor] = messageEvaluator;

            BuildMessage(c_messageDescriptor, messageEvaluator, inProgressMessageEvaluators);
            return messageEvaluator;
        });
    }

    private void BuildMessage(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        try
        {
            // don't process google well-known types.
            // when we use the Accessor in .Net, the value is automatically unwrapped
            // unlike Java, so we shouldn't be dealing with these wrapper types directly at all.
            if (GoogleWellKnownTypes.Contains(messageDescriptor.FullName))
            {
                return;
            }

            var defaultInstance = messageDescriptor.Parser.ParseFrom(Array.Empty<byte>());
            // var descriptor = defaultInstance.Descriptor;
            var messageConstraints = ConstraintResolver.ResolveMessageConstraints(messageDescriptor);
            if (messageConstraints.Disabled)
            {
                return;
            }

            ProcessMessageExpressions(messageDescriptor, messageConstraints, messageEvaluator, defaultInstance);
            ProcessOneofConstraints(messageDescriptor, messageEvaluator);
            ProcessFields(messageDescriptor, messageEvaluator, nestedMessageEvaluators);
        }
        catch (InvalidProtocolBufferException e)
        {
            throw new CompilationException($"Failed to parse proto definition: {messageDescriptor.FullName}.  {e.Message}", e);
        }
    }

    private void ProcessOneofConstraints(MessageDescriptor desc, MessageEvaluator msgEval)
    {
        var oneofs = desc.Oneofs;
        if (oneofs == null)
        {
            return;
        }

        foreach (var oneofDesc in oneofs)
        {
            var oneofConstraints = ConstraintResolver.ResolveOneofConstraints(oneofDesc);
            var oneofEvaluatorEval = new OneofEvaluator(oneofDesc, oneofConstraints.Required);
            msgEval.AddEvaluator(oneofEvaluatorEval);
        }
    }

    private void ProcessFields(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        var fieldDescriptors = messageDescriptor.Fields.InDeclarationOrder();
        foreach (var fieldDescriptor in fieldDescriptors)
        {
            var fieldConstraints = ConstraintResolver.ResolveFieldConstraints(fieldDescriptor);
            var fieldEvaluator = BuildField(fieldDescriptor, fieldConstraints, nestedMessageEvaluators);
            messageEvaluator.AddEvaluator(fieldEvaluator);
        }
    }

    private FieldEvaluator BuildField(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        //we pass in the message descriptor and evaluator here because we don't have our current descriptor stored in the dictionary yet
        //and if we have a nested/recursive data structure, we need to be able to get our current evaluator instance independently of the dictionary to prevent a race condition.

        var valueEvaluatorEval = new ValueEvaluator(fieldConstraints, fieldDescriptor, fieldConstraints.IgnoreEmpty);
        var fieldEvaluator = new FieldEvaluator(valueEvaluatorEval, fieldDescriptor, fieldConstraints.Required, fieldConstraints.IgnoreEmpty || fieldDescriptor.HasPresence);
        BuildValue(fieldDescriptor, fieldConstraints, false, fieldEvaluator.ValueEvaluator, nestedMessageEvaluators);
        return fieldEvaluator;
    }

    private void BuildValue(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluator, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        ProcessFieldExpressions(fieldConstraints, valueEvaluator);
        ProcessEmbeddedMessage(fieldDescriptor, fieldConstraints, forItems, valueEvaluator, nestedMessageEvaluators);
        ProcessWrapperConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator, nestedMessageEvaluators);
        ProcessStandardConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
        ProcessAnyConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
        ProcessEnumConstraints(fieldDescriptor, fieldConstraints, valueEvaluator);
        ProcessMapConstraints(fieldDescriptor, fieldConstraints, valueEvaluator, nestedMessageEvaluators);
        ProcessRepeatedConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator, nestedMessageEvaluators);
    }

    private void ProcessFieldExpressions(FieldConstraints fieldConstraints, ValueEvaluator valueEvaluatorEval)
    {
        IList<Constraint> constraintsCelList = fieldConstraints.Cel;
        if (constraintsCelList.Count == 0)
        {
            return;
        }

        var compiledPrograms = CompileConstraints(constraintsCelList, CelEnvironment);
        if (compiledPrograms.Count > 0)
        {
            valueEvaluatorEval.AddEvaluator(new CompiledProgramsEvaluator(compiledPrograms));
        }
    }

    private void ProcessEmbeddedMessage(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        //we pass in the message descriptor and evaluator here because we don't have our current descriptor stored in the dictionary yet
        //and if we have a nested/recursive data structure, we need to be able to get our current evaluator instance independently of the dictionary to prevent a race condition.

        if (fieldDescriptor.FieldType != FieldType.Message
            || fieldConstraints.Skipped
            || fieldDescriptor.IsMap
            || (fieldDescriptor.IsRepeated && !forItems)
            || GoogleWellKnownTypes.Contains(fieldDescriptor.MessageType.FullName))
        {
            return;
        }

        if (!nestedMessageEvaluators.TryGetValue(fieldDescriptor.MessageType, out var embedEval))
        {
            embedEval = Build(fieldDescriptor.MessageType, nestedMessageEvaluators);
        }

        valueEvaluatorEval.AddEvaluator(embedEval);
    }

    private void ProcessWrapperConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        if (fieldDescriptor.FieldType != FieldType.Message
            || fieldConstraints.Skipped
            || fieldDescriptor.IsMap
            || (fieldDescriptor.IsRepeated && !forItems))
        {
            return;
        }

        var expectedWrapperDescriptor = DescriptorMappings.ExpectedWrapperConstraints(fieldDescriptor.MessageType.FullName);
        if (expectedWrapperDescriptor == null)
        {
            return;
        }

        var constraintDescriptor = FieldConstraints.Descriptor.FindFieldByName(expectedWrapperDescriptor.Name);
        if (constraintDescriptor == null)
        {
            return;
        }

        //should check this is a wrapped constraint.
        var valueFieldDescriptor = fieldDescriptor.MessageType.FindFieldByName("value");
        if (valueFieldDescriptor == null)
        {
            return;
        }

        var unwrapped = new ValueEvaluator(fieldConstraints, fieldDescriptor, fieldConstraints.IgnoreEmpty);
        BuildValue(valueFieldDescriptor, fieldConstraints, true, unwrapped, nestedMessageEvaluators);
        valueEvaluatorEval.AddEvaluator(unwrapped);
    }

    private void ProcessAnyConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval)
    {
        if ((fieldDescriptor.IsRepeated && !forItems)
            || fieldDescriptor.FieldType != FieldType.Message
            || !fieldDescriptor.MessageType.FullName.Equals("google.protobuf.Any"))
        {
            return;
        }

        var typeUrlDesc = fieldDescriptor.MessageType.FindFieldByName("type_url");
        if (typeUrlDesc == null)
        {
            return;
        }

        if (fieldConstraints?.Any?.In == null)
        {
            return;
        }

        var anyEvaluatorEval = new AnyEvaluator(typeUrlDesc, fieldConstraints?.Any?.In, fieldConstraints?.Any?.NotIn);
        valueEvaluatorEval.AddEvaluator(anyEvaluatorEval);
    }

    private void ProcessEnumConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, ValueEvaluator valueEvaluatorEval)
    {
        if (fieldDescriptor.FieldType != FieldType.Enum)
        {
            return;
        }

        if (fieldConstraints.Enum != null && fieldConstraints.Enum.DefinedOnly)
        {
            var enumDescriptor = fieldDescriptor.EnumType;
            valueEvaluatorEval.AddEvaluator(new EnumEvaluator(enumDescriptor.Values));
        }
    }

    private void ProcessMapConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, ValueEvaluator valueEvaluatorEval, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        if (!fieldDescriptor.IsMap)
        {
            return;
        }


        var mapKeyFieldConstraints = fieldConstraints.Map?.Keys ?? new FieldConstraints();
        var mapValuesFieldConstraints = fieldConstraints.Map?.Values ?? new FieldConstraints();

        var mapEval = new MapEvaluator(fieldConstraints, fieldDescriptor);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(1), mapKeyFieldConstraints, true, mapEval.KeyEvaluator, nestedMessageEvaluators);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(2), mapValuesFieldConstraints, true, mapEval.ValueEvaluator, nestedMessageEvaluators);

        valueEvaluatorEval.AddEvaluator(mapEval);
    }

    private void ProcessRepeatedConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval, IDictionary<MessageDescriptor, IEvaluator> nestedMessageEvaluators)
    {
        if (fieldDescriptor.IsMap || !fieldDescriptor.IsRepeated || forItems)
        {
            return;
        }

        var repeatedFieldConstraints = fieldConstraints.Repeated?.Items ?? new FieldConstraints();

        var listEval = new ListEvaluator(fieldConstraints, fieldDescriptor);
        BuildValue(fieldDescriptor, repeatedFieldConstraints, true, listEval.ItemConstraints, nestedMessageEvaluators);
        valueEvaluatorEval.AddEvaluator(listEval);
    }

    private void ProcessStandardConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval)
    {
        var compile = Constraints.Compile(fieldDescriptor, fieldConstraints, forItems);
        if (compile.Count == 0)
        {
            return;
        }

        valueEvaluatorEval.AddEvaluator(new CompiledProgramsEvaluator(compile));
    }

    private void ProcessMessageExpressions(MessageDescriptor messageDescriptor, MessageConstraints msgConstraints, MessageEvaluator msgEval, IMessage message)
    {
        var celList = msgConstraints.Cel;
        if (celList.Count == 0)
        {
            return;
        }

        // Env finalEnv =
        //     env.extend(
        //                EnvOption.types(message),
        //                EnvOption.declarations(
        //                                       Decls.newVar(Variable.THIS_NAME, Decls.newObjectType(desc.getFullName()))));
        var compiledPrograms = CompileConstraints(celList, CelEnvironment);
        if (compiledPrograms.Count == 0)
        {
            throw new CompilationException("Compile returned null");
        }

        msgEval.AddEvaluator(new CompiledProgramsEvaluator(compiledPrograms));
    }

    private static List<CompiledProgram> CompileConstraints(IList<Constraint> constraints, CelEnvironment env)
    {
        var expressions = Expression.FromConstraints(constraints);
        var compiledPrograms = new List<CompiledProgram>();
        foreach (var expression in expressions)
        {
            var expressionDelegate = env.Compile(expression.ExpressionText);
            compiledPrograms.Add(new CompiledProgram(expressionDelegate, null, expression));
        }

        return compiledPrograms;
    }
}