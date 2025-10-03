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
using Cel;
using Google.Protobuf;
using ProtoValidate.Exceptions;
using ProtoValidate.Internal.Evaluator;

namespace ProtoValidate.Internal.Cel;

internal class CompiledProgram
{
    public CelProgramDelegate CelProgramDelegate { get; }
    public Expression Source { get; }
    public IMessage? Rules { get; }
    public object? Rule { get; }
    public FieldPath? RulePath { get; }

    public CompiledProgram(CelProgramDelegate celExpressionDelegate, Expression? source, FieldPath? rulePath, IMessage? rules, object? rule)
    {
        CelProgramDelegate = celExpressionDelegate ?? throw new ArgumentNullException(nameof(celExpressionDelegate));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Rules = rules;
        Rule = rule;
        RulePath = rulePath;
    }

    public Violation? Eval(IValue? value, IDictionary<string, object?> variables)
    {
        object? evalResult;

        try
        {
            //set the rules
            if (Rules != null)
            {
                variables["rules"] = Rules;
            }
            else
            {
                variables.Remove("rules");
            }

            if (Rule != null)
            {
                variables["rule"] = Rule;
            }
            else
            {
                variables.Remove("rule");
            }
            evalResult = CelProgramDelegate?.Invoke(variables);
        }
        catch (Exception x)
        {
            throw new ExecutionException($"Error evaluating {Source.Id}", x, Source);
        }

        if (evalResult is string evalResultString)
        {
            if (string.IsNullOrWhiteSpace(evalResultString))
            {
                return null;
            }

            var violation = new Violation
            {
                RuleId = Source.Id,
                Message = evalResultString,
            };

            if (RulePath != null)
            {
                violation.Rule = new FieldPath() { Elements = { RulePath.Elements.Select(c => c.Clone()) } };
            }

            if (value != null)
            {
                violation.Value = value;
            }

            return violation;
        }

        if (evalResult is bool evalResultBool)
        {
            if (evalResultBool)
            {
                return null;
            }

            var violation = new Violation
            {
                RuleId = Source.Id,
                Message = Source.Message
            };

            if (RulePath != null)
            {
                violation.Rule = new FieldPath() { Elements = { RulePath.Elements.Select(c => c.Clone()) } };
            }

            if (value != null)
            {
                violation.Value = value;
            }

            return violation;
        }

        throw new ExecutionException($"Resolved to an unexpected type {evalResult?.GetType().FullName ?? "null"}", Source);
    }
}