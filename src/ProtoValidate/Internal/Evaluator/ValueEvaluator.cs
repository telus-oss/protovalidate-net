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

using System.Globalization;
using System.Reflection;
using Buf.Validate;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class ValueEvaluator : IEvaluator
{
    public FieldConstraints FieldConstraints { get; }
    public FieldDescriptor FieldDescriptor { get; }
    public Ignore Ignore { get; }
    private object? DefaultValue { get; }
    public List<IEvaluator> Evaluators { get; } = new();

    public ValueEvaluator(FieldConstraints fieldConstraints, FieldDescriptor fieldDescriptor, Ignore ignore)
    {
        FieldConstraints = fieldConstraints ?? throw new ArgumentNullException(nameof(fieldConstraints));
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
        Ignore = ignore;
        DefaultValue = fieldDescriptor.GetDefaultValue();
    }
    
    public override string ToString()
    {
        return $"Value Evaluator: {FieldDescriptor.FullName}";
    }

    public void AddEvaluator(IEvaluator evaluator)
    {
        if (evaluator == null)
        {
            throw new ArgumentNullException(nameof(evaluator));
        }

        Evaluators.Add(evaluator);
    }


    public bool Tautology => Evaluators.Count > 0;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (Ignore == Ignore.IfUnpopulated || Ignore == Ignore.IfDefaultValue)
        {
            return ValidationResult.Empty;
        }

            if (Ignore == Ignore.IfUnpopulated && IsDefaultValue(value.Value<object?>()))
            {
                if (!FieldDescriptor.HasPresence)
                {
                    return ValidationResult.Empty;
                }
                if (FieldDescriptor.ContainingType != null && FieldDescriptor.ContainingType.IsMapEntry)
                {
                    return ValidationResult.Empty;
                }
            }

            if (Ignore == Ignore.IfDefaultValue && IsDefaultValue(value.Value<object?>()))
            {
                return ValidationResult.Empty;
            }
        }


        var violations = new List<Violation>();
        foreach (var evaluator in Evaluators)
        {
            var evalResult = evaluator.Evaluate(value, failFast);

            if (failFast && !evalResult.IsSuccess)
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

    private bool IsDefaultValue(object? val)
    {
        if (val == null)
        {
            return false;
        }

        try
        {
            if (val is string stringFieldValue)
            {
                if (string.IsNullOrEmpty(DefaultValue?.ToString()) && string.IsNullOrEmpty(stringFieldValue))
                {
                    return true;
                }
                if (string.Equals(DefaultValue?.ToString(), stringFieldValue, StringComparison.Ordinal))
                {
                    return true;
                }

                return false;
            }

            if (val is ByteString byteStringFieldValue && byteStringFieldValue.Length == 0)
            {
                //check this for default value
                return true;
            }


            if (val is IMessage innerMessage)
            {
                if (innerMessage.CalculateSize() == 0)
                {
                    return true;
                }
            }
            
            if (ValueEquality(val, DefaultValue))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static bool ValueEquality(object val1, object? val2)
    {
        if (!(val1 is IConvertible))
        {
            return false;
        }

        if (!(val2 is IConvertible))
        {
            return false;
        }

        // convert val2 to type of val1.
        var converted2 = Convert.ChangeType(val2, val1.GetType());

        // compare now that same type.
        return val1.Equals(converted2);
    }
}