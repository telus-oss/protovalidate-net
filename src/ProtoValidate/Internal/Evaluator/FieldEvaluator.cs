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

using System.Collections;
using Buf.Validate;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class FieldEvaluator : IEvaluator
{
    public ValueEvaluator ValueEvaluator { get; }
    private FieldDescriptor Descriptor { get; }


    /// <summary>
    ///     Indicates that the field must have a set value
    /// </summary>
    private bool Required { get; }

    /// <summary>
    ///     Indicates that the evaluators should not be applied to this field if the value is unset. Fields
    ///     that contain messages, are prefixed with `optional`, or are part of a oneof are considered
    ///     optional. evaluators will still be applied if the field is set as the zero value.
    /// </summary>
    private bool Optional { get; }

    public FieldEvaluator(ValueEvaluator valueEvaluator, FieldDescriptor descriptor, bool required, bool optional)
    {
        ValueEvaluator = valueEvaluator ?? throw new ArgumentNullException(nameof(valueEvaluator));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Required = required;
        Optional = optional;
    }

    public override string ToString()
    {
        return $"Field Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology => !Required && ValueEvaluator.Tautology;


    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var message = value?.MessageValue;
        if (message == null)
        {
            return ValidationResult.Empty;
        }

        bool hasField;
        if (Descriptor.IsMap)
        {
            var list = (IDictionary)Descriptor.Accessor.GetValue(message);
            hasField = list.Count > 0;
        }
        else if (Descriptor.IsRepeated)
        {
            var list = (IList)Descriptor.Accessor.GetValue(message);
            hasField = list.Count > 0;
        }
        else if (Descriptor.HasPresence)
        {
            hasField = Descriptor.Accessor.HasValue(message);
        }
        else
        {
            hasField = true;
        }

        if (Required && !hasField)
        {
            return new ValidationResult(new[]
            {
                new Violation
                {
                    ConstraintId = "required",
                    Message = "Value is required.",
                    FieldPath = Descriptor.Name
                }
            });
        }

        if ((Optional || ValueEvaluator.IgnoreEmpty) && !hasField)
        {
            return ValidationResult.Empty;
        }

        var fieldValue = Descriptor.Accessor.GetValue(message);

        if (ValueEvaluator.IgnoreEmpty)
        {
            if (fieldValue is string stringFieldValue)
            {
                //strings are always initialized even if they have no value
                //so we need to check their length for the IgnoreEmpty flag.
                if (string.IsNullOrEmpty(stringFieldValue))
                {
                    return ValidationResult.Empty;
                }
            }

            if (fieldValue is ByteString byteStringFieldValue)
            {
                //ByteStrings are always initialized even if they have no value
                //so we need to check their length for the IgnoreEmpty flag.
                if (byteStringFieldValue.Length == 0)
                {
                    return ValidationResult.Empty;
                }
            }
        }

        var evalResult = ValueEvaluator.Evaluate(new ObjectValue(Descriptor, fieldValue), failFast);
        var violations = evalResult.Violations.PrefixErrorPaths("{0}", Descriptor.Name);

        return new ValidationResult(violations);
    }
}