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

internal class MessageEvaluator : IEvaluator
{
    public List<IEvaluator> Evaluators { get; } = new();
    private MessageDescriptor Descriptor { get; }

    public MessageEvaluator(MessageDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public void AddEvaluator(IEvaluator evaluator)
    {
        if (evaluator == null)
        {
            throw new ArgumentNullException(nameof(evaluator));
        }

        Evaluators.Add(evaluator);
    }

    public override string ToString()
    {
        return $"Message Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology
    {
        get
        {
            foreach (var evaluator in Evaluators)
            {
                if (!evaluator.Tautology)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var violations = new List<Violation>();
        foreach (var evaluator in Evaluators)
        {
            var evalResult = evaluator.Evaluate(value, failFast);
            if (failFast && evalResult.Violations.Count > 0)
            {
                return evalResult;
            }

            violations.AddRange(evalResult.Violations);
        }

        if (violations.Count == 0)
        {
            return ValidationResult.Empty;
        }

        return new ValidationResult(violations);
    }
}