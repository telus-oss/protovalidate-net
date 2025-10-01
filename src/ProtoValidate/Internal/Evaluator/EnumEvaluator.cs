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
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class EnumEvaluator : IEvaluator
{
    internal IList<EnumValueDescriptor> ValueDescriptors { get; }
    internal Dictionary<int, EnumValueDescriptor> Values { get; }

    public EnumEvaluator(IList<EnumValueDescriptor> valueDescriptors)
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
            return new ValidationResult(new[]
            {
                new Violation
                {
                    RuleId = "enum.defined_only",
                    Message = "Value must be one of the defined enum values."
                }
            });
        }

        return ValidationResult.Empty;
    }
}