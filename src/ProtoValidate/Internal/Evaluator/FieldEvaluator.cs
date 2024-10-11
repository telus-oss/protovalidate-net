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
    private FieldDescriptor FieldDescriptor { get; }
    private FieldConstraints FieldConstraints { get; }

    private Ignore Ignore { get; }
    public FieldEvaluator(ValueEvaluator valueEvaluator, FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints)
    {
        ValueEvaluator = valueEvaluator ?? throw new ArgumentNullException(nameof(valueEvaluator));
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
        FieldConstraints = fieldConstraints ?? throw new ArgumentNullException(nameof(fieldConstraints));
        Ignore = fieldConstraints.CalculateIgnore(fieldDescriptor);
    }

    public override string ToString()
    {
        return $"Field Evaluator: {FieldDescriptor.FullName}";
    }

    public bool Tautology => !FieldConstraints.Required && ValueEvaluator.Tautology;


    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var message = value?.MessageValue;
        if (message == null)
        {
            return ValidationResult.Empty;
        }

        var fieldValue = FieldDescriptor.Accessor.GetValue(message); ;
        bool hasField;

        if (FieldDescriptor.IsMap)
        {
            var list = (IDictionary)fieldValue;
            hasField = list.Count > 0;
        }
        else if (FieldDescriptor.IsRepeated)
        {
            var list = (IList)fieldValue;
            hasField = list.Count > 0;
        }
        else if (FieldDescriptor.HasPresence)
        {
            hasField = FieldDescriptor.Accessor.HasValue(message);
        }
        else
        {
            //this logic is to support the "Required" has field.
            if (fieldValue is string stringFieldValue && string.IsNullOrEmpty(stringFieldValue))
            {
                hasField = false;
            }
            else if (fieldValue is ByteString byteStringFieldValue && byteStringFieldValue.Length == 0)
            {
                hasField = false;
            }
            else
            {
                hasField = true;
            }
        }

        if (FieldConstraints.Required && !hasField)
        {
            return new ValidationResult(new[]
            {
                new Violation
                {
                    ConstraintId = "required",
                    Message = "Value is required.",
                    FieldPath = FieldDescriptor.Name
                }
            });
        }

        if ((Ignore == Ignore.IfUnpopulated || Ignore == Ignore.IfDefaultValue || FieldDescriptor.HasPresence) && !hasField)
        {
            return ValidationResult.Empty;
        }
        
        var evalResult = ValueEvaluator.Evaluate(new ObjectValue(FieldDescriptor, fieldValue), failFast);
        var violations = evalResult.Violations.PrefixErrorPaths("{0}", FieldDescriptor.Name);

        return new ValidationResult(violations);
    }
}