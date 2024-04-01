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
    private ConstraintResolver ConstraintResolver { get; } = new();
    private readonly ConcurrentDictionary<MessageDescriptor, IEvaluator> EvaluatorMap = new();
    private bool DisableLazy { get; }
    private CelEnvironment CelEnvironment { get; }
    private ConstraintCache Constraints { get; }

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

    public EvaluatorBuilder(CelEnvironment celEnvironment, bool disableLazy)
    {
        DisableLazy = disableLazy;
        CelEnvironment = celEnvironment;
        Constraints = new ConstraintCache(celEnvironment);
    }

    /// <summary>
    ///     Returns a pre-cached Evaluator for the given descriptor or, if the descriptor is
    ///     unknown, returns an evaluator that always throws a CompilationException.
    /// </summary>
    /// <param name="desc"></param>
    /// <returns></returns>
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
        if (EvaluatorMap.TryGetValue(messageDescriptor, out var evaluator))
        {
            return evaluator;
        }

        return Build(messageDescriptor);
    }

    private IEvaluator Build(MessageDescriptor messageDescriptor)
    {
        if (EvaluatorMap.TryGetValue(messageDescriptor, out var evaluator))
        {
            return evaluator;
        }

        var messageEvaluator = new MessageEvaluator(messageDescriptor);
        EvaluatorMap.TryAdd(messageDescriptor, messageEvaluator);
        BuildMessage(messageDescriptor, messageEvaluator);
        return messageEvaluator;
    }

    private void BuildMessage(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator)
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
            ProcessFields(messageDescriptor, messageEvaluator);
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

            if (oneofConstraints is null) continue;

            var oneofEvaluatorEval = new OneofEvaluator(oneofDesc, oneofConstraints.Required);
            msgEval.AddEvaluator(oneofEvaluatorEval);
        }
    }

    private void ProcessFields(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator)
    {
        var fieldDescriptors = messageDescriptor.Fields.InDeclarationOrder();
        foreach (var fieldDescriptor in fieldDescriptors)
        {
            var fieldConstraints = ConstraintResolver.ResolveFieldConstraints(fieldDescriptor);

            if (fieldConstraints is null) continue;

            var fieldEvaluator = BuildField(fieldDescriptor, fieldConstraints);
            messageEvaluator.AddEvaluator(fieldEvaluator);
        }
    }

    private FieldEvaluator BuildField(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints)
    {
        var valueEvaluatorEval = new ValueEvaluator(fieldConstraints, fieldDescriptor, fieldConstraints.IgnoreEmpty);
        var fieldEvaluator = new FieldEvaluator(valueEvaluatorEval, fieldDescriptor, fieldConstraints.Required, fieldConstraints.IgnoreEmpty || fieldDescriptor.HasPresence);
        BuildValue(fieldDescriptor, fieldConstraints, false, fieldEvaluator.ValueEvaluator);
        return fieldEvaluator;
    }

    private void BuildValue(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluator)
    {
        ProcessFieldExpressions(fieldDescriptor, fieldConstraints, valueEvaluator);
        ProcessEmbeddedMessage(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
        ProcessWrapperConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
        ProcessStandardConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
        ProcessAnyConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
        ProcessEnumConstraints(fieldDescriptor, fieldConstraints, valueEvaluator);
        ProcessMapConstraints(fieldDescriptor, fieldConstraints, valueEvaluator);
        ProcessRepeatedConstraints(fieldDescriptor, fieldConstraints, forItems, valueEvaluator);
    }

    private void ProcessFieldExpressions(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, ValueEvaluator valueEvaluatorEval)
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

    private void ProcessEmbeddedMessage(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval)
    {
        if (fieldDescriptor.FieldType != FieldType.Message
            || fieldConstraints.Skipped
            || fieldDescriptor.IsMap
            || (fieldDescriptor.IsRepeated && !forItems)
            || GoogleWellKnownTypes.Contains(fieldDescriptor.MessageType.FullName))
        {
            return;
        }

        var embedEval = Build(fieldDescriptor.MessageType);
        valueEvaluatorEval.AddEvaluator(embedEval);
    }

    private void ProcessWrapperConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval)
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
        BuildValue(valueFieldDescriptor, fieldConstraints, true, unwrapped);
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

    private void ProcessMapConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, ValueEvaluator valueEvaluatorEval)
    {
        if (!fieldDescriptor.IsMap)
        {
            return;
        }


        var mapKeyFieldConstraints = fieldConstraints.Map?.Keys ?? new FieldConstraints();
        var mapValuesFieldConstraints = fieldConstraints.Map?.Values ?? new FieldConstraints();

        var mapEval = new MapEvaluator(fieldConstraints, fieldDescriptor);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(1), mapKeyFieldConstraints, true, mapEval.KeyEvaluator);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(2), mapValuesFieldConstraints, true, mapEval.ValueEvaluator);

        valueEvaluatorEval.AddEvaluator(mapEval);
    }

    private void ProcessRepeatedConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems, ValueEvaluator valueEvaluatorEval)
    {
        if (fieldDescriptor.IsMap || !fieldDescriptor.IsRepeated || forItems)
        {
            return;
        }

        var repeatedFieldConstraints = fieldConstraints.Repeated?.Items ?? new FieldConstraints();

        var listEval = new ListEvaluator(fieldConstraints, fieldDescriptor);
        BuildValue(fieldDescriptor, repeatedFieldConstraints, true, listEval.ItemConstraints);
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