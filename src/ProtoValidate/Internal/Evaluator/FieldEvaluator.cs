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

using System.Collections;
using Buf.Validate;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

internal class FieldEvaluator : IEvaluator
{
    public ValueEvaluator ValueEvaluator { get; }
    private FieldDescriptor FieldDescriptor { get; }
    private FieldRules FieldRules { get; }
    private Ignore Ignore { get; }
    private RuleViolationHelper RuleViolationHelper { get; }
    private FieldPath RequiredRulePath { get; }

    public FieldEvaluator(ValueEvaluator valueEvaluator, FieldDescriptor fieldDescriptor, FieldRules fieldRules, MessageRules messageRules)
    {
        ValueEvaluator = valueEvaluator ?? throw new ArgumentNullException(nameof(valueEvaluator));
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
        FieldRules = fieldRules ?? throw new ArgumentNullException(nameof(fieldRules));
        if (messageRules == null)
        {
            throw new ArgumentNullException(nameof(messageRules));
        }
        Ignore = fieldRules.CalculateIgnore(fieldDescriptor, messageRules);

        RequiredRulePath = new FieldPath()
        {
            Elements =
            {
                FieldRules.Descriptor.FindFieldByNumber(FieldRules.RequiredFieldNumber).CreateFieldPathElement()
            }
        };
        RuleViolationHelper = new RuleViolationHelper(valueEvaluator);
    }

    public override string ToString()
    {
        return $"Field Evaluator: {FieldDescriptor.FullName}";
    }

    public bool Tautology => !FieldRules.Required && ValueEvaluator.Tautology;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var message = value?.MessageValue;
        if (message == null)
        {
            return ValidationResult.Empty;
        }

        if (Ignore == Ignore.Always)
        {
            return ValidationResult.Empty;
        }

        var fieldValue = FieldDescriptor.Accessor.GetValue(message);
        bool hasField;

        if (FieldDescriptor.IsMap)
        {
            var list = (IDictionary)fieldValue;
            hasField = list.Count > 0;
        }
        else if (FieldDescriptor.IsRepeated)
        {
            var list = (IList)fieldValue;
            hasField = list.Count > 0;
        }
        else if (FieldDescriptor.HasPresence)
        {
            hasField = FieldDescriptor.Accessor.HasValue(message);
        }
        else
        {
            //this logic is to support the "Required" has field.
            if (fieldValue is string stringFieldValue && string.IsNullOrEmpty(stringFieldValue))
            {
                hasField = false;
            }
            else if (fieldValue is ByteString byteStringFieldValue && byteStringFieldValue.Length == 0)
            {
                hasField = false;
            }
            else
            {
                hasField = true;
            }
        }

        if (FieldRules.Required && !hasField)
        {
            var violation = new Violation
            {
                RuleId = "required",
                Message = "value is required",
                Rule = new FieldPath(),
                Field = new FieldPath()
            };
            violation.UpdatePaths(RuleViolationHelper.FieldPathElement, RuleViolationHelper.RulePrefixElements);
            violation.Rule.Elements.AddRange(RequiredRulePath.Elements);
            
            return new ValidationResult([violation]);
        }

        if ((Ignore == Ignore.IfZeroValue || FieldDescriptor.HasPresence) && !hasField)
        {
            return ValidationResult.Empty;
        }

        var evalResult = ValueEvaluator.Evaluate(new ObjectValue(FieldDescriptor, fieldValue), failFast);
        
        var violations = evalResult.Violations;
     
        return new ValidationResult(violations);
    }
}