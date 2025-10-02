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

public class MapEvaluator : IEvaluator
{
    public MapEvaluator(FieldRules fieldRules, FieldDescriptor fieldDescriptor, MessageRules messageRules)
    {
        if (fieldRules == null)
        {
            throw new ArgumentNullException(nameof(fieldRules));
        }

        if (fieldDescriptor == null)
        {
            throw new ArgumentNullException(nameof(fieldDescriptor));
        }

        FieldRules = fieldRules;
        FieldDescriptor = fieldDescriptor;

        var mapRules = fieldRules.Map;
        var keyDescriptor = fieldDescriptor.MessageType.FindFieldByNumber(1);
        var valueDescriptor = fieldDescriptor.MessageType.FindFieldByNumber(2);

        var mapRulesKeysFieldRules = mapRules?.Keys ?? new FieldRules();
        var mapRulesValuesFieldRules = mapRules?.Values ?? new FieldRules();

        KeyEvaluator = new ValueEvaluator(mapRulesKeysFieldRules, keyDescriptor, mapRulesKeysFieldRules.CalculateIgnore(keyDescriptor, messageRules));
        ValueEvaluator = new ValueEvaluator(mapRulesValuesFieldRules, valueDescriptor, mapRulesValuesFieldRules.CalculateIgnore(valueDescriptor, messageRules));
    }

    public ValueEvaluator KeyEvaluator { get; }
    public ValueEvaluator ValueEvaluator { get; }
    public FieldDescriptor FieldDescriptor { get; }
    public FieldRules FieldRules { get; }

    public bool Tautology => KeyEvaluator.Tautology && ValueEvaluator.Tautology;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            return ValidationResult.Empty;
        }

        var violations = new List<Violation>();
        var mapValue = value.MapValue();

        foreach (var entry in mapValue)
        {
            violations.AddRange(EvalPairs(entry.Key, entry.Value, failFast));
            if (failFast && violations.Count > 0)
            {
                return new ValidationResult(violations);
            }
        }

        if (violations.Count == 0)
        {
            return ValidationResult.Empty;
        }

        return new ValidationResult(violations);
    }

    public override string ToString()
    {
        return $"Map Evaluator {FieldDescriptor.FullName}";
    }

    internal List<Violation> EvalPairs(IValue key, IValue value, bool failFast)
    {
        var keyViolations = KeyEvaluator.Evaluate(key, failFast).Violations;
        List<Violation> valueViolations;
        if (failFast && keyViolations.Count > 0)
        {
            // Don't evaluate value constraints if failFast is enabled and keys failed validation.
            // We still need to continue execution to the end to properly prefix violation field paths.
            valueViolations = new List<Violation>();
        }
        else
        {
            valueViolations = ValueEvaluator.Evaluate(value, failFast).Violations;
        }

        if (keyViolations.Count == 0 && valueViolations.Count == 0)
        {
            return new List<Violation>();
        }

        var violations = new List<Violation>(keyViolations.Count + valueViolations.Count);
        violations.AddRange(keyViolations);
        violations.AddRange(valueViolations);

        var keyName = key.Value<object?>();
        if (keyName == null)
        {
            return new List<Violation>();
        }

        List<Violation> prefixedViolations;
        if (keyName.IsNumber())
        {
            prefixedViolations = violations.PrefixErrorPaths("[{0}]", keyName);
        }
        else
        {
            prefixedViolations = violations.PrefixErrorPaths("[\"{0}\"]", keyName);
        }

        return prefixedViolations;
    }
}