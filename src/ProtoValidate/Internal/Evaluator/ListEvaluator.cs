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

public class ListEvaluator : IEvaluator
{
    public ValueEvaluator ItemConstraints { get; }
    public FieldDescriptor FieldDescriptor { get; }
    public FieldConstraints FieldConstraints { get; }


    public ListEvaluator(FieldConstraints fieldConstraints, FieldDescriptor fieldDescriptor)
    {
        FieldConstraints = fieldConstraints ?? throw new ArgumentNullException(nameof(fieldConstraints));
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));

        var ignoreEmpty = false;
        if (fieldConstraints.Repeated?.Items?.IgnoreEmpty != null)
        {
            ignoreEmpty = fieldConstraints.Repeated.Items.IgnoreEmpty;
        }

        ItemConstraints = new ValueEvaluator(fieldConstraints, fieldDescriptor, ignoreEmpty);
    }

    public override string ToString()
    {
        return $"List Evaluator: {FieldDescriptor.FullName}";
    }

    public bool Tautology => ItemConstraints.Tautology;

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
            var evalResult = ItemConstraints.Evaluate(repeatedValues[i], failFast);
            if (evalResult.Violations.Count == 0)
            {
                continue;
            }

            var violations = evalResult.Violations.PrefixErrorPaths("[{0}]", i);
            if (failFast && violations.Count > 0)
            {
                return evalResult;
            }

            allViolations.AddRange(violations);
        }

        return new ValidationResult(allViolations);
    }
}