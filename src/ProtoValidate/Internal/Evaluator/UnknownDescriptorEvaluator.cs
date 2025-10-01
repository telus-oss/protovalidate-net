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

public class UnknownDescriptorEvaluator : IEvaluator
{
    internal DescriptorBase Descriptor { get; }

    public UnknownDescriptorEvaluator(DescriptorBase descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        Descriptor = descriptor;
    }

    public override string ToString()
    {
        return $"UnknownDescriptorEvaluator Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology => false;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ValidationResult(new[]
        {
            new Violation
            {
                Message = $"No evaluator available for {Descriptor.FullName}."
            }
        });
    }
}