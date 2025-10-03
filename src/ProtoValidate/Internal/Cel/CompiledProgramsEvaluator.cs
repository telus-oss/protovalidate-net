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
using ProtoValidate.Internal.Evaluator;

namespace ProtoValidate.Internal.Cel;

internal class CompiledProgramsEvaluator : IEvaluator
{
    public List<CompiledProgram> CompiledPrograms { get; }
    private RuleViolationHelper RuleViolationHelper { get; }
   
    public CompiledProgramsEvaluator(ValueEvaluator? valueEvaluator, List<CompiledProgram> compiledPrograms)
    {
        CompiledPrograms = compiledPrograms ?? throw new ArgumentNullException(nameof(compiledPrograms));
        RuleViolationHelper = new RuleViolationHelper(valueEvaluator);
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
            // Console.WriteLine($"Evaluating rule '{compiledProgram.Source.Id}': {compiledProgram.Source.ExpressionText}");

            var violation = compiledProgram.Eval(value, variables);
            if (violation != null)
            {
                violationList.Add(violation);
            }
            
            if (failFast && violationList.Count > 0)
            {
                break;
            }
        }

        violationList.UpdatePaths(RuleViolationHelper.FieldPathElement, RuleViolationHelper.RulePrefixElements);

        return new ValidationResult(violationList);
    }
}