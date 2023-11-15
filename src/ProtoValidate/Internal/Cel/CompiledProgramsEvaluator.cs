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
using ProtoValidate.Internal.Evaluator;

namespace ProtoValidate.Internal.Cel;

public class CompiledProgramsEvaluator : IEvaluator
{
    public List<CompiledProgram> CompiledPrograms { get; }

    public CompiledProgramsEvaluator(List<CompiledProgram> compiledPrograms)
    {
        CompiledPrograms = compiledPrograms ?? throw new ArgumentNullException(nameof(compiledPrograms));
    }

    public override string ToString()
    {
        return $"CompiledPrograms Evaluator: {CompiledPrograms.Count}";
    }

    public bool Tautology => CompiledPrograms.Count == 0;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var variables = new Dictionary<string, object?>();
        variables.Add("this", value?.Value<object?>());
        variables.Add("now", DateTimeOffset.UtcNow);

        var violationList = new List<Violation>();

        foreach (var compiledProgram in CompiledPrograms)
        {
            //Console.WriteLine($"Evaluating rule '{compiledProgram.Source.Id}': {compiledProgram.Source.ExpressionText}");

            var violation = compiledProgram.Eval(variables);
            if (violation != null)
            {
                violation.Value = value?.Value<object?>();
                //Console.WriteLine($"  Rule found violation: {violation}");
                violationList.Add(violation);
            }
            //Console.WriteLine();

            if (failFast)
            {
                break;
            }
        }

        return new ValidationResult(violationList);
    }
}