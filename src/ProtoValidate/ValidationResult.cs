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
        public static ValidationResult Empty { get; } = new();

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
                //fix
                if (!string.IsNullOrEmpty(violation.Field.ToString()))
                {
                    //fix
                    builder.Append(violation.Field.ToString());
                    builder.Append(": ");
                }

                builder.Append(string.Format("{0} [{1}]", violation.Message, violation.RuleId));
            }

            return builder.ToString();
        }
    }
}