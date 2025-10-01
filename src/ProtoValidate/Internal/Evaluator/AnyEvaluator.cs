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

public class AnyEvaluator : IEvaluator
{
    internal FieldDescriptor TypeUrlDescriptor { get; }
    internal Dictionary<string, object?>? InLookup { get; }
    internal Dictionary<string, object?>? NotInLookup { get; }

    public AnyEvaluator(FieldDescriptor typeUrlDescriptor, IList<string>? inList, IList<string>? notInList)
    {
        TypeUrlDescriptor = typeUrlDescriptor ?? throw new ArgumentNullException(nameof(typeUrlDescriptor));

        //convert to a dictionary for faster performance than O(n)
        if (inList != null)
        {
            InLookup = inList.ToDictionary(c => c, c => (object?)null);
        }

        if (notInList != null)
        {
            NotInLookup = notInList.ToDictionary(c => c, c => (object?)null);
        }
    }

    public override string ToString()
    {
        return $"AnyEvaluator In: {InLookup?.Count ?? 0} NotIn: {NotInLookup?.Count ?? 0}";
    }

    public bool Tautology => InLookup != null && InLookup.Count == 0 && NotInLookup != null && NotInLookup.Count == 0;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var anyValue = value?.MessageValue;
        if (anyValue == null)
        {
            return ValidationResult.Empty;
        }

        var violationList = new List<Violation>();

        var typeUrl = (string)TypeUrlDescriptor.Accessor.GetValue(anyValue);

        if (InLookup != null && InLookup.Count > 0 && !InLookup.ContainsKey(typeUrl))
        {
            var violation = new Violation
            {
                RuleId = "any.in",
                Message = "Type url must be in the allow list."
            };

            violationList.Add(violation);
            if (failFast)
            {
                return new ValidationResult(violationList);
            }
        }

        if (NotInLookup != null && NotInLookup.Count > 0 && NotInLookup.ContainsKey(typeUrl))
        {
            var violation = new Violation
            {
                RuleId = "any.not_in",
                Message = "Type url must not be in the block list."
            };

            violationList.Add(violation);
        }

        return new ValidationResult(violationList);
    }
}