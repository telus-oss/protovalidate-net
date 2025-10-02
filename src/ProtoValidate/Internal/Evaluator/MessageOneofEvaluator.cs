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

using Buf.Validate;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class MessageOneofEvaluator : IEvaluator
{
    public List<IEvaluator> Evaluators { get; } = new();
    public MessageDescriptor Descriptor { get; }
    public FieldDescriptor[] FieldDescriptors { get; }
    public MessageOneofRule MessageOneOfRule { get; }

    public MessageOneofEvaluator(MessageDescriptor descriptor, MessageOneofRule messageOneOfRule)
    {
        Descriptor = descriptor;
        MessageOneOfRule = messageOneOfRule;
        FieldDescriptors = messageOneOfRule.Fields.Select(descriptor.FindFieldByName).ToArray();
    }

    public override string ToString()
    {
        return $"Message Oneof Rule Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology => false;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value?.MessageValue == null)
        {
            return ValidationResult.Empty;
        }

        var message = (IMessage)value.MessageValue;
        int hasCount = 0;

        foreach (var fieldDescriptor in FieldDescriptors)
        {
            if (fieldDescriptor.HasPresence && fieldDescriptor.Accessor.HasValue(message))
            {
                hasCount += 1;
            }
            else
            {
                var fieldValue = fieldDescriptor.Accessor.GetValue(message);
                if (!ValueEvaluator.IsDefaultValue(fieldValue, fieldDescriptor.GetDefaultValue()))
                {
                    hasCount += 1;
                }
            }
        }

        var violations = new List<Violation>();

        if (hasCount > 1)
        {
            var fieldNames = string.Join(", ", FieldDescriptors.Select(c => c.Name));
            var violation = new Violation
            {
                RuleId = "message.oneof",
                Message = $"only one of {fieldNames} can be set"
            };
            violations.Add(violation);
        }

        if (MessageOneOfRule.Required && hasCount == 0)
        {
            var fieldNames = string.Join(", ", FieldDescriptors.Select(c => c.Name));
            var violation = new Violation
            {
                RuleId = "message.oneof",
                Message = $"one of {fieldNames} must be set"
            };
            violations.Add(violation);
        }

        if (violations.Count == 0)
        {
            return ValidationResult.Empty;
        }

        return new ValidationResult(violations);
    }
}