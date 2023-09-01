using System.Globalization;
using Buf.Validate;

namespace ProtoValidate.Internal.Evaluator;

public static class ErrorPathUtils
{
    public static List<Violation> PrefixErrorPaths(this List<Violation> violations, string format, params object?[] args)
    {
        var prefix = string.Format(CultureInfo.InvariantCulture, format, args);
        return violations.Select(c =>
                                 {
                                     var fieldPath = c.FieldPath;
                                     var prefixedFieldPath = "";
                                     if (string.IsNullOrEmpty(fieldPath))
                                     {
                                         prefixedFieldPath = fieldPath;
                                     }
                                     else if (fieldPath.StartsWith("[", StringComparison.Ordinal))
                                     {
                                         prefixedFieldPath = prefix + prefixedFieldPath;
                                     }
                                     else
                                     {
                                         prefixedFieldPath = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", prefix, fieldPath);
                                     }

                                     var violation = c.Clone();
                                     violation.FieldPath = prefixedFieldPath;

                                     return violation;
                                 }
                                ).ToList();
    }
}