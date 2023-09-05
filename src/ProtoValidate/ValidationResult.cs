using System.Text;
using Buf.Validate;

namespace Buf.Validate
{
    public partial class Violation
    {
        public object? Value { get; set; }
    }
}

namespace ProtoValidate
{

    public class ValidationResult
    {
        public static ValidationResult Empty { get; } = new ValidationResult();

        public ValidationResult() { }

        public ValidationResult(IEnumerable<Violation> violations)
        {
            if (violations == null)
            {
                throw new ArgumentNullException(nameof(violations));
            }

            Violations.AddRange(violations);
        }

        public List<Violation> Violations { get; } = new();

        public bool IsSuccess => Violations.Count == 0;

        public override string ToString()
        {
            if (IsSuccess)
            {
                return "Validation success";
            }

            var builder = new StringBuilder();

            builder.Append("Validation error: ");
            foreach (var violation in Violations)
            {
                builder.Append("\n - ");
                if (!string.IsNullOrEmpty(violation.FieldPath))
                {
                    builder.Append(violation.FieldPath);
                    builder.Append(": ");
                }

                builder.Append(string.Format("{0} [{1}]", violation.Message, violation.ConstraintId));
            }

            return builder.ToString();
        }
    }
}