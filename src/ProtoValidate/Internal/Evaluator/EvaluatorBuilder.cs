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
    internal static readonly string[] GoogleWellKnownTypes =
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

    internal readonly ConcurrentDictionary<string, IEvaluator> EvaluatorMap = new();

    public EvaluatorBuilder(CelEnvironment celEnvironment, bool disableLazy, IList<FieldDescriptor> extensions)
    {
        DisableLazy = disableLazy;
        Extensions = extensions;
        CelEnvironment = celEnvironment;
        Constraints = new ConstraintCache(celEnvironment, extensions);
        RuleResolver = new RuleResolver();
    }

    internal IList<FieldDescriptor> Extensions { get; }

    internal RuleResolver RuleResolver { get; }
    internal bool DisableLazy { get; }
    internal CelEnvironment CelEnvironment { get; }
    internal ConstraintCache Constraints { get; }

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

    internal IEvaluator LoadDescriptor(MessageDescriptor messageDescriptor)
    {
        if (!EvaluatorMap.TryGetValue(messageDescriptor.FullName, out var evaluator))
        {
            return new UnknownDescriptorEvaluator(messageDescriptor);
        }

        return evaluator;
    }

    internal IEvaluator LoadOrBuildDescriptor(MessageDescriptor messageDescriptor)
    {
        return EvaluatorMap.GetOrAdd(messageDescriptor.FullName, _ => Build(messageDescriptor));
    }

    internal IEvaluator Build(MessageDescriptor messageDescriptor)
    {
        return Build(messageDescriptor, new Dictionary<string, IEvaluator>());
    }

    internal IEvaluator Build(MessageDescriptor messageDescriptor, IDictionary<string, IEvaluator> inProgressMessageEvaluators)
    {
        return EvaluatorMap.GetOrAdd(messageDescriptor.FullName, c_messageDescriptor =>
        {
            var messageEvaluator = new MessageEvaluator(messageDescriptor);

            //when we are building messages, they could be recursive on themselves
            //we use this dictionary to track that
            //so that the EvaluatorMap can remain thread-safe and won't get updated until the evaluators are fully built.
            //doing this keeps those nested evaluators on the stack (thread-safe) until we need them.
            inProgressMessageEvaluators[c_messageDescriptor] = messageEvaluator;

            BuildMessage(messageDescriptor, messageEvaluator, inProgressMessageEvaluators);
            return messageEvaluator;
        });
    }

    internal void BuildMessage(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator, IDictionary<string, IEvaluator> nestedMessageEvaluators)
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

            if (messageDescriptor.Parser == null)
            {
                return;
            }

            var defaultInstance = messageDescriptor.Parser.ParseFrom(Array.Empty<byte>());

            // var descriptor = defaultInstance.Descriptor;
            var messageConstraints = RuleResolver.ResolveMessageRules(messageDescriptor);
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

    internal void ProcessOneofConstraints(MessageDescriptor desc, MessageEvaluator msgEval)
    {
        var oneofs = desc.Oneofs;
        if (oneofs == null)
        {
            return;
        }

        foreach (var oneofDesc in oneofs)
        {
            var oneofConstraints = RuleResolver.ResolveOneofRules(oneofDesc);
            var oneofEvaluatorEval = new OneofEvaluator(oneofDesc, oneofConstraints.Required);
            msgEval.AddEvaluator(oneofEvaluatorEval);
        }
    }

    internal void ProcessFields(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        var fieldDescriptors = messageDescriptor.Fields.InDeclarationOrder();
        foreach (var fieldDescriptor in fieldDescriptors)
        {
            var fieldRules = RuleResolver.ResolveFieldRules(fieldDescriptor);

            var fieldEvaluator = BuildField(fieldDescriptor, fieldRules, nestedMessageEvaluators);
            messageEvaluator.AddEvaluator(fieldEvaluator);
        }
    }

    internal FieldEvaluator BuildField(FieldDescriptor fieldDescriptor, FieldRules fieldRules, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        //we pass in the message descriptor and evaluator here because we don't have our current descriptor stored in the dictionary yet
        //and if we have a nested/recursive data structure, we need to be able to get our current evaluator instance independently of the dictionary to prevent a race condition.
        var valueEvaluatorEval = new ValueEvaluator(fieldRules, fieldDescriptor, fieldRules.CalculateIgnore(fieldDescriptor));

        var fieldEvaluator = new FieldEvaluator(valueEvaluatorEval, fieldDescriptor, fieldRules);
        BuildValue(fieldDescriptor, fieldRules, false, fieldEvaluator.ValueEvaluator, nestedMessageEvaluators);
        return fieldEvaluator;
    }

    internal void BuildValue(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems, ValueEvaluator valueEvaluator, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        ProcessFieldExpressions(fieldRules, valueEvaluator);
        ProcessEmbeddedMessage(fieldDescriptor, fieldRules, forItems, valueEvaluator, nestedMessageEvaluators);
        ProcessWrapperConstraints(fieldDescriptor, fieldRules, forItems, valueEvaluator, nestedMessageEvaluators);
        ProcessStandardConstraints(fieldDescriptor, fieldRules, forItems, valueEvaluator);
        ProcessAnyConstraints(fieldDescriptor, fieldRules, forItems, valueEvaluator);
        ProcessEnumConstraints(fieldDescriptor, fieldRules, valueEvaluator);
        ProcessMapConstraints(fieldDescriptor, fieldRules, valueEvaluator, nestedMessageEvaluators);
        ProcessRepeatedConstraints(fieldDescriptor, fieldRules, forItems, valueEvaluator, nestedMessageEvaluators);
    }

    internal void ProcessFieldExpressions(FieldRules fieldRules, ValueEvaluator valueEvaluatorEval)
    {
        IList<Rule> rulesCelList = fieldRules.Cel;
        if (rulesCelList.Count == 0)
        {
            return;
        }

        var compiledPrograms = CompileRules(rulesCelList, CelEnvironment);
        if (compiledPrograms.Count > 0)
        {
            valueEvaluatorEval.AddEvaluator(new CompiledProgramsEvaluator(compiledPrograms));
        }
    }

    internal void ProcessEmbeddedMessage(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems, ValueEvaluator valueEvaluatorEval, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        //we pass in the message descriptor and evaluator here because we don't have our current descriptor stored in the dictionary yet
        //and if we have a nested/recursive data structure, we need to be able to get our current evaluator instance independently of the dictionary to prevent a race condition.

        if (fieldDescriptor.FieldType != FieldType.Message
            || (fieldRules.CalculateIgnore(fieldDescriptor) == Ignore.Always)
            || fieldDescriptor.IsMap
            || (fieldDescriptor.IsRepeated && !forItems)
            || GoogleWellKnownTypes.Contains(fieldDescriptor.MessageType.FullName))
        {
            return;
        }

        if (!nestedMessageEvaluators.TryGetValue(fieldDescriptor.MessageType.FullName, out var embedEval))
        {
            embedEval = Build(fieldDescriptor.MessageType, nestedMessageEvaluators);
        }

        valueEvaluatorEval.AddEvaluator(embedEval);
    }

    internal void ProcessWrapperConstraints(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems, ValueEvaluator valueEvaluatorEval, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        if (fieldDescriptor.FieldType != FieldType.Message
            || (fieldRules.CalculateIgnore(fieldDescriptor) == Ignore.Always)
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

        var constraintDescriptor = FieldRules.Descriptor.FindFieldByName(expectedWrapperDescriptor.Name);
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

        var unwrapped = new ValueEvaluator(fieldRules, fieldDescriptor, fieldRules.CalculateIgnore(fieldDescriptor));
        BuildValue(valueFieldDescriptor, fieldRules, true, unwrapped, nestedMessageEvaluators);
        valueEvaluatorEval.AddEvaluator(unwrapped);
    }

    internal void ProcessAnyConstraints(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems, ValueEvaluator valueEvaluatorEval)
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

        if (fieldRules?.Any?.In == null)
        {
            return;
        }

        var anyEvaluatorEval = new AnyEvaluator(typeUrlDesc, fieldRules?.Any?.In, fieldRules?.Any?.NotIn);
        valueEvaluatorEval.AddEvaluator(anyEvaluatorEval);
    }

    internal void ProcessEnumConstraints(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval)
    {
        if (fieldDescriptor.FieldType != FieldType.Enum)
        {
            return;
        }

        if (fieldRules.Enum != null && fieldRules.Enum.DefinedOnly)
        {
            var enumDescriptor = fieldDescriptor.EnumType;
            valueEvaluatorEval.AddEvaluator(new EnumEvaluator(enumDescriptor.Values));
        }
    }

    internal void ProcessMapConstraints(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        if (!fieldDescriptor.IsMap)
        {
            return;
        }
        
        var mapKeyFieldRules = fieldRules.Map?.Keys ?? new FieldRules();
        var mapValuesFieldRules = fieldRules.Map?.Values ?? new FieldRules();

        var mapEval = new MapEvaluator(fieldRules, fieldDescriptor);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(1), mapKeyFieldRules, true, mapEval.KeyEvaluator, nestedMessageEvaluators);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(2), mapValuesFieldRules, true, mapEval.ValueEvaluator, nestedMessageEvaluators);

        valueEvaluatorEval.AddEvaluator(mapEval);
    }

    internal void ProcessRepeatedConstraints(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems, ValueEvaluator valueEvaluatorEval, IDictionary<string, IEvaluator> nestedMessageEvaluators)
    {
        if (fieldDescriptor.IsMap || !fieldDescriptor.IsRepeated || forItems)
        {
            return;
        }

        var repeatedFieldRules = fieldRules.Repeated?.Items ?? new FieldRules();

        var listEval = new ListEvaluator(fieldRules, fieldDescriptor);
        BuildValue(fieldDescriptor, repeatedFieldRules, true, listEval.ItemConstraints, nestedMessageEvaluators);
        valueEvaluatorEval.AddEvaluator(listEval);
    }

    internal void ProcessStandardConstraints(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems, ValueEvaluator valueEvaluatorEval)
    {
        var compile = Constraints.Compile(fieldDescriptor, fieldRules, forItems);
        if (compile.Count == 0)
        {
            return;
        }

        valueEvaluatorEval.AddEvaluator(new CompiledProgramsEvaluator(compile));
    }

    internal void ProcessMessageExpressions(MessageDescriptor messageDescriptor, MessageRules messageRules, MessageEvaluator msgEval, IMessage message)
    {
        var celList = messageRules.Cel;
        if (celList.Count == 0)
        {
            return;
        }

        var compiledPrograms = CompileRules(celList, CelEnvironment);
        if (compiledPrograms.Count == 0)
        {
            throw new CompilationException("Compile returned null");
        }

        msgEval.AddEvaluator(new CompiledProgramsEvaluator(compiledPrograms));
    }

    internal static List<CompiledProgram> CompileRules(IList<Rule> rules, CelEnvironment env)
    {
        var expressions = Expression.FromRules(rules);
        var compiledPrograms = new List<CompiledProgram>();
        foreach (var expression in expressions)
        {
            var expressionDelegate = env.Compile(expression.ExpressionText);
            compiledPrograms.Add(new CompiledProgram(expressionDelegate, null, null, expression));
        }

        return compiledPrograms;
    }
}