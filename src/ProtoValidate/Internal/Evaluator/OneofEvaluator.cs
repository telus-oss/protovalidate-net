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

public class OneofEvaluator : IEvaluator
{
    private OneofDescriptor Descriptor { get; }
    private bool Required { get; }

    public OneofEvaluator(OneofDescriptor descriptor, bool required)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Required = required;
    }

    public override string ToString()
    {
        return $"OneOf Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology => !Required;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var message = value.MessageValue;
        if (message == null)
        {
            return ValidationResult.Empty;
        }

        var caseFieldDescriptor = Descriptor.Accessor.GetCaseFieldDescriptor(message);

        if (Required && caseFieldDescriptor == null)
        {
            return new ValidationResult(new[]
            {
                new Violation
                {
                    ConstraintId = "required",
                    Message = "Exactly one field is required in oneof."
                }
            });
        }

        return ValidationResult.Empty;
    }
}