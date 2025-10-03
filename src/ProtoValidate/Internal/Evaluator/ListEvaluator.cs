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

using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

internal class ListEvaluator : IEvaluator
{
    public ListEvaluator(ValueEvaluator valueEvaluator, FieldRules fieldRules, FieldDescriptor fieldDescriptor, MessageRules messageRules)
    {
        if (valueEvaluator == null)
        {
            throw new ArgumentNullException(nameof(valueEvaluator));
        }

        FieldRules = fieldRules ?? throw new ArgumentNullException(nameof(fieldRules));
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
        if (messageRules == null)
        {
            throw new ArgumentNullException(nameof(messageRules));
        }

        var ignore = fieldRules.Repeated?.Items?.CalculateIgnore(fieldDescriptor, messageRules) ?? Ignore.Unspecified;

        var repeatedItemsRulePath = new FieldPath();
        repeatedItemsRulePath.Elements.Add(FieldRules.Descriptor.FindFieldByNumber(FieldRules.RepeatedFieldNumber).CreateFieldPathElement());
        repeatedItemsRulePath.Elements.Add(RepeatedRules.Descriptor.FindFieldByNumber(RepeatedRules.ItemsFieldNumber).CreateFieldPathElement());

        ItemValueEvaluator = new ValueEvaluator(fieldRules, fieldDescriptor, ignore, repeatedItemsRulePath, null);
        RuleViolationHelper = new RuleViolationHelper(valueEvaluator);
    }
    private RuleViolationHelper RuleViolationHelper { get; }
    public ValueEvaluator ItemValueEvaluator { get; }
    private FieldDescriptor FieldDescriptor { get; }
    private FieldRules FieldRules { get; }
    public bool Tautology => ItemValueEvaluator.Tautology;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            return ValidationResult.Empty;
        }

        var allViolations = new List<Violation>();

        var repeatedValues = value.RepeatedValue();

        for (var i = 0; i < repeatedValues.Count; i++)
        {
            var evalResult = ItemValueEvaluator.Evaluate(repeatedValues[i], failFast);
            if (evalResult.Violations.Count == 0)
            {
                continue;
            }

            var violations = evalResult.Violations;

            //ste the violation paths
            var fieldPathElement = RuleViolationHelper.FieldPathElement!.Clone();
            fieldPathElement.Index = Convert.ToUInt64(i);
            violations.UpdatePaths(fieldPathElement, RuleViolationHelper.RulePrefixElements);

            if (failFast && violations.Count > 0)
            {
                return evalResult;
            }

            allViolations.AddRange(violations);
        }

        return new ValidationResult(allViolations);
    }

    public override string ToString()
    {
        return $"List Evaluator: {FieldDescriptor.FullName}";
    }
}