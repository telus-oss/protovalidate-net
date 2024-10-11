// Copyright 2024 TELUS
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal
{
    internal static class FieldConstraintExtensions
    {
        public static Ignore CalculateIgnore(this FieldConstraints fieldConstraints, FieldDescriptor fieldDescriptor)
        {
            if (fieldDescriptor == null)
            {
                throw new ArgumentNullException(nameof(fieldDescriptor));
            }
#pragma warning disable CS0612 // Type or member is obsolete
            if (fieldConstraints.Ignore == Ignore.Unspecified)
            {
                if (fieldConstraints.Skipped)
                {
                    return Ignore.Always;
                }

                if (fieldConstraints.IgnoreEmpty)
                {
                    return Ignore.IfUnpopulated;
                }

                if (fieldDescriptor.HasPresence)
                {
                    if (fieldDescriptor.ContainingType == null || !fieldDescriptor.ContainingType.IsMapEntry)
                    {
                        return Ignore.IfUnpopulated;
                    }
                }
            }
            else if (fieldConstraints.Ignore == Ignore.Empty)
            {
                return Ignore.IfUnpopulated;
            }
            else if (fieldConstraints.Ignore == Ignore.Default)
            {
                return Ignore.IfDefaultValue;
            }

            return fieldConstraints.Ignore;
#pragma warning restore CS0612 // Type or member is obsolete
        }
    }
}
