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
                                         prefixedFieldPath = prefix;
                                     }
                                     else if (fieldPath.StartsWith("[", StringComparison.Ordinal))
                                     {
                                         prefixedFieldPath = prefix + fieldPath;
                                     }
                                     else
                                     {
                                         prefixedFieldPath = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", prefix, fieldPath);
                                     }

                                     var violation = c.Clone();
                                     violation.FieldPath = prefixedFieldPath;
                                     violation.Value = c.Value;

                                     return violation;
                                 }
                                ).ToList();
    }
}