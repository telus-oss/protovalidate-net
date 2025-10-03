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

internal class EnumEvaluator : IEvaluator
{
    private IList<EnumValueDescriptor> ValueDescriptors { get; }
    private Dictionary<int, EnumValueDescriptor> Values { get; }
    private RuleViolationHelper RuleViolationHelper { get; }
    private FieldPath DefinedOnlyRulePath { get; }
    public EnumEvaluator(ValueEvaluator valueEvaluator, IList<EnumValueDescriptor> valueDescriptors)
    {
        if (valueDescriptors == null)
        {
            throw new ArgumentNullException(nameof(valueDescriptors));
        }

        ValueDescriptors = valueDescriptors;
        Values = new Dictionary<int, EnumValueDescriptor>();

        foreach (var descriptor in valueDescriptors)
        {
            Values[descriptor.Number] = descriptor;
        }

        DefinedOnlyRulePath = new FieldPath()
        {
            Elements =
            {
                FieldRules.Descriptor.FindFieldByNumber(FieldRules.EnumFieldNumber).CreateFieldPathElement(),
                EnumRules.Descriptor.FindFieldByNumber(EnumRules.DefinedOnlyFieldNumber).CreateFieldPathElement()
            }
        };
        RuleViolationHelper = new RuleViolationHelper(valueEvaluator);
    }

    public override string ToString()
    {
        return "Enum Evaluator";
    }

    public bool Tautology => false;

    /// <summary>
    ///     Evaluates an enum value.
    /// </summary>
    /// <param name="value">The value to evaluate</param>
    /// <param name="failFast">Indicates if the evaluation should stop on the first violation.</param>
    /// <returns></returns>
    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var enumValue = value?.Value<object?>();
        if (enumValue == null)
        {
            return ValidationResult.Empty;
        }

        var enumIntValue = (int)enumValue;

        if (!Values.ContainsKey(enumIntValue))
        {
            var violation = new Violation
            {
                RuleId = "enum.defined_only",
                Message = "value must be one of the defined enum values",
                Rule = new FieldPath(),
                Field = new FieldPath(),
                Value = enumValue
            };
            violation.UpdatePaths(RuleViolationHelper.FieldPathElement, RuleViolationHelper.RulePrefixElements);
            violation.Rule.Elements.AddRange(DefinedOnlyRulePath.Elements);

            return new ValidationResult([violation]);
        }

        return ValidationResult.Empty;
    }
}