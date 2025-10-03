using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate
{
    internal static class ViolationExtensions
    {
        public static Violation CreateViolation(FieldDescriptor field, string ruleId, string message)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentNullException(nameof(ruleId));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var fieldPath = new FieldPath();
            fieldPath.Elements.Add(new FieldPathElement()
            {
                FieldName = field.Name,
                FieldNumber = field.FieldNumber,
                FieldType = field.ToProto().Type
            });

            return new Violation
            {
                Rule = fieldPath,
                RuleId = ruleId,
                Message = message,
                Field = fieldPath
            };
        }
    }
}
