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

internal class MapEvaluator : IEvaluator
{
    public MapEvaluator(FieldRules fieldRules, FieldDescriptor fieldDescriptor, MessageRules messageRules, ValueEvaluator mapEvaluator)
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
        KeyDescriptor = fieldDescriptor.MessageType.FindFieldByNumber(1);
        ValueDescriptor = fieldDescriptor.MessageType.FindFieldByNumber(2);

        var mapRulesKeysFieldRules = mapRules?.Keys ?? new FieldRules();
        var mapRulesValuesFieldRules = mapRules?.Values ?? new FieldRules();

        var mapKeysRulePath = new FieldPath();
        mapKeysRulePath.Elements.Add(FieldRules.Descriptor.FindFieldByNumber(FieldRules.MapFieldNumber).CreateFieldPathElement());
        mapKeysRulePath.Elements.Add(MapRules.Descriptor.FindFieldByNumber(MapRules.KeysFieldNumber).CreateFieldPathElement());

        var mapValuesRulePath = new FieldPath();
        mapValuesRulePath.Elements.Add(FieldRules.Descriptor.FindFieldByNumber(FieldRules.MapFieldNumber).CreateFieldPathElement());
        mapValuesRulePath.Elements.Add(MapRules.Descriptor.FindFieldByNumber(MapRules.ValuesFieldNumber).CreateFieldPathElement());

        KeyEvaluator = new ValueEvaluator(mapRulesKeysFieldRules, KeyDescriptor, mapRulesKeysFieldRules.CalculateIgnore(KeyDescriptor, messageRules), mapKeysRulePath, null);
        ValueEvaluator = new ValueEvaluator(mapRulesValuesFieldRules, ValueDescriptor, mapRulesValuesFieldRules.CalculateIgnore(ValueDescriptor, messageRules), mapValuesRulePath, null);

        RuleViolationHelper = new RuleViolationHelper(mapEvaluator);
    }

    private RuleViolationHelper RuleViolationHelper { get; }
    private FieldDescriptor KeyDescriptor { get; }
    private FieldDescriptor ValueDescriptor { get; }
    public ValueEvaluator KeyEvaluator { get; }
    public ValueEvaluator ValueEvaluator { get; }
    private FieldDescriptor FieldDescriptor { get; }
    private FieldRules FieldRules { get; }

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
        var keyName = key.Value<object?>();
        if (keyName == null)
        {
            return new List<Violation>();
        }

        List<Violation> valueViolations;
        var keyViolations = KeyEvaluator.Evaluate(key, failFast).Violations;
        if (failFast && keyViolations.Count > 0)
        {
            // Don't evaluate value rules if failFast is enabled and keys failed validation.
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

        var fieldPathElement = RuleViolationHelper.FieldPathElement!.Clone();
        fieldPathElement.KeyType = KeyDescriptor.ToProto().Type;
        fieldPathElement.ValueType = ValueDescriptor.ToProto().Type;

        switch (KeyDescriptor.FieldType)
        {
            case FieldType.Int64:
            case FieldType.Int32:
            case FieldType.SInt64:
            case FieldType.SInt32:
            case FieldType.SFixed64:
            case FieldType.SFixed32:
                fieldPathElement.IntKey = Convert.ToInt64(keyName);
                break;
            case FieldType.UInt64:
            case FieldType.UInt32:
            case FieldType.Fixed64:
            case FieldType.Fixed32:
                fieldPathElement.UintKey = Convert.ToUInt64(keyName);
                break;
            case FieldType.Bool:
                fieldPathElement.BoolKey = (bool)keyName;
                break;
            case FieldType.String:
                fieldPathElement.StringKey = (string)keyName;
                break;
        }


        foreach (var violation in (keyViolations))
        {
            violation.ForKey = true;
        }
        violations.UpdatePaths(fieldPathElement, RuleViolationHelper.RulePrefixElements);

        return violations;
    }
}