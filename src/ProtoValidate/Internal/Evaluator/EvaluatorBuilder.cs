// Copyright 2023-2025 TELUS
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
using ProtoValidate.Internal.Rules;

namespace ProtoValidate.Internal.Evaluator;

internal class EvaluatorBuilder
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

    internal readonly ConcurrentDictionary<string, MessageEvaluator> EvaluatorMap = new();

    public EvaluatorBuilder(CelEnvironment celEnvironment, bool disableLazy, IList<FieldDescriptor> extensions)
    {
        DisableLazy = disableLazy;
        Extensions = extensions;
        CelEnvironment = celEnvironment;
        Rules = new RuleCache(celEnvironment, extensions);
        RuleResolver = new RuleResolver();
    }

    internal IList<FieldDescriptor> Extensions { get; }

    internal RuleResolver RuleResolver { get; }
    internal bool DisableLazy { get; }
    internal CelEnvironment CelEnvironment { get; }
    internal RuleCache Rules { get; }

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

    internal MessageEvaluator LoadOrBuildDescriptor(MessageDescriptor messageDescriptor)
    {
        return EvaluatorMap.GetOrAdd(messageDescriptor.FullName, _ => Build(messageDescriptor));
    }

    internal MessageEvaluator Build(MessageDescriptor messageDescriptor)
    {
        return Build(messageDescriptor, new Dictionary<string, MessageEvaluator>());
    }

    internal MessageEvaluator Build(MessageDescriptor messageDescriptor, IDictionary<string, MessageEvaluator> inProgressMessageEvaluators)
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

    internal void BuildMessage(MessageDescriptor messageDescriptor, MessageEvaluator messageEvaluator, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
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
            var messageRules = RuleResolver.ResolveMessageRules(messageDescriptor);

            ProcessMessageExpressions(messageDescriptor, messageRules, messageEvaluator, defaultInstance);
            ProcessMessageOneofRules(messageDescriptor, messageRules, messageEvaluator);
            ProcessOneofRules(messageDescriptor, messageEvaluator);
            ProcessFields(messageDescriptor, messageRules, messageEvaluator, nestedMessageEvaluators);
        }
        catch (InvalidProtocolBufferException e)
        {
            throw new CompilationException($"Failed to parse proto definition: {messageDescriptor.FullName}.  {e.Message}", e);
        }
    }

    internal void ProcessMessageOneofRules(MessageDescriptor messageDescriptor, MessageRules messageRules, MessageEvaluator messageEvaluator)
    {
        foreach (var messageOneofRule in messageRules.Oneof)
        {
            var oneofEvaluatorEval = new MessageOneofEvaluator(messageDescriptor, messageOneofRule);
            messageEvaluator.AddEvaluator(oneofEvaluatorEval);
        }
    }

    internal void ProcessOneofRules(MessageDescriptor desc, MessageEvaluator msgEval)
    {
        var oneofs = desc.Oneofs;
        if (oneofs == null)
        {
            return;
        }

        foreach (var oneofDesc in oneofs)
        {
            var oneofRules = RuleResolver.ResolveOneofRules(oneofDesc);
            var oneofEvaluatorEval = new OneofEvaluator(oneofDesc, oneofRules.Required);
            msgEval.AddEvaluator(oneofEvaluatorEval);
        }
    }

    internal void ProcessFields(MessageDescriptor messageDescriptor, MessageRules messageRules, MessageEvaluator messageEvaluator, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        var fieldDescriptors = messageDescriptor.Fields.InDeclarationOrder();
        foreach (var fieldDescriptor in fieldDescriptors)
        {
            var fieldRules = RuleResolver.ResolveFieldRules(fieldDescriptor);

            var fieldEvaluator = BuildField(fieldDescriptor, fieldRules, messageRules, nestedMessageEvaluators);
            messageEvaluator.AddEvaluator(fieldEvaluator);
        }
    }

    internal FieldEvaluator BuildField(FieldDescriptor fieldDescriptor, FieldRules fieldRules, MessageRules messageRules, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        FieldPathElement fieldPathElement = fieldDescriptor.CreateFieldPathElement();

        //we pass in the message descriptor and evaluator here because we don't have our current descriptor stored in the dictionary yet
        //and if we have a nested/recursive data structure, we need to be able to get our current evaluator instance independently of the dictionary to prevent a race condition.
        var valueEvaluatorEval = new ValueEvaluator(fieldRules, fieldDescriptor, fieldRules.CalculateIgnore(fieldDescriptor, messageRules), null, fieldPathElement);

        var fieldEvaluator = new FieldEvaluator(valueEvaluatorEval, fieldDescriptor, fieldRules, messageRules);
        BuildValue(fieldDescriptor, fieldRules, fieldEvaluator.ValueEvaluator, messageRules, nestedMessageEvaluators);
        return fieldEvaluator;
    }

    internal void BuildValue(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluator, MessageRules messageRules, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        ProcessFieldExpressions(fieldDescriptor, fieldRules, valueEvaluator);
        ProcessEmbeddedMessage(fieldDescriptor, fieldRules, valueEvaluator, messageRules, nestedMessageEvaluators);
        ProcessWrapperRules(fieldDescriptor, fieldRules, valueEvaluator, messageRules, nestedMessageEvaluators);
        ProcessStandardRules(fieldDescriptor, fieldRules, valueEvaluator);
        ProcessAnyRules(fieldDescriptor, fieldRules, valueEvaluator);
        ProcessEnumRules(fieldDescriptor, fieldRules, valueEvaluator);
        ProcessMapRules(fieldDescriptor, fieldRules, valueEvaluator, messageRules, nestedMessageEvaluators);
        ProcessRepeatedRules(fieldDescriptor, fieldRules, valueEvaluator, messageRules, nestedMessageEvaluators);
    }

    internal void ProcessFieldExpressions(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval)
    {
        IList<Rule> rulesCelList = fieldRules.Cel;
        if (rulesCelList.Count == 0)
        {
            return;
        }

        var compiledPrograms = CompileRules(rulesCelList, CelEnvironment, true);
        if (compiledPrograms.Count > 0)
        {
            valueEvaluatorEval.AddEvaluator(new CompiledProgramsEvaluator(valueEvaluatorEval, compiledPrograms));
        }
    }

    internal void ProcessEmbeddedMessage(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval, MessageRules messageRules, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        //we pass in the message descriptor and evaluator here because we don't have our current descriptor stored in the dictionary yet
        //and if we have a nested/recursive data structure, we need to be able to get our current evaluator instance independently of the dictionary to prevent a race condition.

        if (fieldDescriptor.FieldType != FieldType.Message
            || (fieldRules.CalculateIgnore(fieldDescriptor, messageRules) == Ignore.Always)
            || fieldDescriptor.IsMap
            || (fieldDescriptor.IsRepeated && valueEvaluatorEval.NestedRule == null)
            || GoogleWellKnownTypes.Contains(fieldDescriptor.MessageType.FullName))
        {
            return;
        }

        if (!nestedMessageEvaluators.TryGetValue(fieldDescriptor.MessageType.FullName, out var embedEval))
        {
            embedEval = Build(fieldDescriptor.MessageType, nestedMessageEvaluators);
        }

        var embeddedMessageEvaluator = new EmbeddedMessageEvaluator(valueEvaluatorEval, embedEval);

        valueEvaluatorEval.AddEvaluator(embeddedMessageEvaluator);
    }

    internal void ProcessWrapperRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval, MessageRules messageRules, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        if (fieldDescriptor.FieldType != FieldType.Message
            || (fieldRules.CalculateIgnore(fieldDescriptor, messageRules) == Ignore.Always)
            || fieldDescriptor.IsMap
            || (fieldDescriptor.IsRepeated && valueEvaluatorEval.NestedRule == null))
        {
            return;
        }

        var expectedWrapperDescriptor = DescriptorMappings.ExpectedWrapperRules(fieldDescriptor.MessageType.FullName);
        if (expectedWrapperDescriptor == null)
        {
            return;
        }

        var ruleDescriptor = FieldRules.Descriptor.FindFieldByName(expectedWrapperDescriptor.Name);
        if (ruleDescriptor == null)
        {
            return;
        }

        //should check this is a wrapped rule.
        var valueFieldDescriptor = fieldDescriptor.MessageType.FindFieldByName("value");
        if (valueFieldDescriptor == null)
        {
            return;
        }

        var fieldPathElement = valueEvaluatorEval.FieldPathElement;

        var unwrapped = new ValueEvaluator(fieldRules, fieldDescriptor, fieldRules.CalculateIgnore(fieldDescriptor, messageRules), valueEvaluatorEval.NestedRule, fieldPathElement);
        BuildValue(valueFieldDescriptor, fieldRules, unwrapped, messageRules, nestedMessageEvaluators);
        valueEvaluatorEval.AddEvaluator(unwrapped);
    }

    internal void ProcessAnyRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval)
    {
        if ((fieldDescriptor.IsRepeated && valueEvaluatorEval.NestedRule == null)
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

        var anyEvaluatorEval = new AnyEvaluator(valueEvaluatorEval, typeUrlDesc, fieldRules?.Any?.In, fieldRules?.Any?.NotIn);
        valueEvaluatorEval.AddEvaluator(anyEvaluatorEval);
    }

    internal void ProcessEnumRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval)
    {
        if (fieldDescriptor.FieldType != FieldType.Enum)
        {
            return;
        }

        if (fieldRules.Enum != null && fieldRules.Enum.DefinedOnly)
        {
            var enumDescriptor = fieldDescriptor.EnumType;
            valueEvaluatorEval.AddEvaluator(new EnumEvaluator(valueEvaluatorEval, enumDescriptor.Values));
        }
    }

    internal void ProcessMapRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval, MessageRules messageRules, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        if (!fieldDescriptor.IsMap)
        {
            return;
        }

        var mapKeyFieldRules = fieldRules.Map?.Keys ?? new FieldRules();
        var mapValuesFieldRules = fieldRules.Map?.Values ?? new FieldRules();

        var mapEval = new MapEvaluator(fieldRules, fieldDescriptor, messageRules, valueEvaluatorEval);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(1), mapKeyFieldRules, mapEval.KeyEvaluator, messageRules, nestedMessageEvaluators);
        BuildValue(fieldDescriptor.MessageType.FindFieldByNumber(2), mapValuesFieldRules, mapEval.ValueEvaluator, messageRules, nestedMessageEvaluators);

        valueEvaluatorEval.AddEvaluator(mapEval);
    }

    internal void ProcessRepeatedRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval, MessageRules messageRules, IDictionary<string, MessageEvaluator> nestedMessageEvaluators)
    {
        if (fieldDescriptor.IsMap || !fieldDescriptor.IsRepeated || valueEvaluatorEval.NestedRule != null)
        {
            return;
        }

        var repeatedFieldRules = fieldRules.Repeated?.Items ?? new FieldRules();

        var listEval = new ListEvaluator(valueEvaluatorEval, fieldRules, fieldDescriptor, messageRules);
        BuildValue(fieldDescriptor, repeatedFieldRules, listEval.ItemValueEvaluator, messageRules, nestedMessageEvaluators);
        valueEvaluatorEval.AddEvaluator(listEval);
    }

    internal void ProcessStandardRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, ValueEvaluator valueEvaluatorEval)
    {
        var fieldPathElement = valueEvaluatorEval.FieldPathElement;

        var compile = Rules.Compile(fieldDescriptor, valueEvaluatorEval.NestedRule != null, fieldRules);
        if (compile.Count == 0)
        {
            return;
        }

        valueEvaluatorEval.AddEvaluator(new CompiledProgramsEvaluator(valueEvaluatorEval, compile));
    }

    internal void ProcessMessageExpressions(MessageDescriptor messageDescriptor, MessageRules messageRules, MessageEvaluator msgEval, IMessage message)
    {
        var celList = messageRules.Cel;
        if (celList.Count == 0)
        {
            return;
        }

        var compiledPrograms = CompileRules(celList, CelEnvironment, false);
        if (compiledPrograms.Count == 0)
        {
            throw new CompilationException("Compile returned null");
        }

        msgEval.AddEvaluator(new CompiledProgramsEvaluator(null, compiledPrograms));
    }

    internal static List<CompiledProgram> CompileRules(IList<Rule> rules, CelEnvironment env, bool isField)
    {
        var expressions = Expression.FromRules(rules).ToList();
        var compiledPrograms = new List<CompiledProgram>();

        for (var i = 0; i < expressions.Count; i++)
        {
            FieldPath? rulePath = null;

            if (isField)
            {
                var fieldPathElement = FieldRules.Descriptor.FindFieldByNumber(FieldRules.CelFieldNumber).CreateFieldPathElement();
                fieldPathElement.Index = Convert.ToUInt64(i);

                rulePath = new FieldPath()
                {
                    Elements = { fieldPathElement }
                };
            }
            
            var expression = expressions[i];
            var expressionDelegate = env.Compile(expression.ExpressionText);
            compiledPrograms.Add(new CompiledProgram(expressionDelegate, expression, rulePath, null, null));
        }

        return compiledPrograms;
    }
}