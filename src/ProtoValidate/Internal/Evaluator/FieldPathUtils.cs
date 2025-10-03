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

internal static class FieldPathUtils
{
    public static FieldPathElement CreateFieldPathElement(this FieldDescriptor fieldDescriptor)
    {
        if (fieldDescriptor == null)
        {
            throw new ArgumentNullException(nameof(fieldDescriptor));
        }
        string name;
        if (fieldDescriptor.IsExtension)
        {
            name = "[" + fieldDescriptor.FullName + "]";
        }
        else
        {
            name = fieldDescriptor.Name;
        }

        var fieldPathElement = new FieldPathElement
        {
            FieldName = name,
            FieldNumber = fieldDescriptor.FieldNumber,
        
            //group syntax is for legacy proto2 compatibility.
            FieldType = fieldDescriptor.FieldType == FieldType.Group ? FieldDescriptorProto.Types.Type.Group : fieldDescriptor.ToProto().Type
        };
        return fieldPathElement;
    }

    public static List<Violation> UpdatePaths(this List<Violation> violations, FieldPathElement? fieldPathElement, List<FieldPathElement> rulePathElements)
    {
        if (fieldPathElement != null || rulePathElements.Count > 0)
        {
            foreach (var violation in violations)
            {
                violation.UpdatePaths(fieldPathElement, rulePathElements);
            }
        }

        return violations;
    }

    public static Violation UpdatePaths(this Violation violation, FieldPathElement? fieldPathElement, List<FieldPathElement> rulePathElements)
    {
        if (fieldPathElement != null || rulePathElements.Count > 0)
        {
            if (violation.Rule == null && rulePathElements.Count > 0)
            {
                violation.Rule = new FieldPath();
            }

            for (var i = rulePathElements.Count - 1; i >= 0; i--)
            {
                violation.Rule!.Elements.Insert(0, rulePathElements[i].Clone());
            }

            if (violation.Field == null && fieldPathElement != null)
            {
                violation.Field = new FieldPath();
            }

            if (fieldPathElement != null)
            {
                violation.Field!.Elements.Insert(0, fieldPathElement.Clone());
            }
        }

        return violation;
    }
    // public static List<Violation> SetViolationFieldPathElement(this List<Violation> violations, FieldPathElement? fieldPathElement)
    // {
    //     if (fieldPathElement == null)
    //     {
    //         return violations;
    //     }
    //
    //     foreach (var violation in violations)
    //     {
    //         violation.SetViolationFieldPathElement(fieldPathElement);
    //     }
    //
    //     return violations;
    // }
    // public static Violation SetViolationFieldPathElement(this Violation violation, FieldPathElement? fieldPathElement)
    // {
    //     if (fieldPathElement == null)
    //     {
    //         return violation;
    //     }
    //
    //     violation.Field ??= new FieldPath();
    //     violation.Field.Elements.Insert(0, fieldPathElement.Clone());
    //
    //     return violation;
    // }
}