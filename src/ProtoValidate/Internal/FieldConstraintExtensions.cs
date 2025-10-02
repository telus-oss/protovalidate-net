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
        public static Ignore CalculateIgnore(this FieldRules fieldRules, FieldDescriptor fieldDescriptor, MessageRules messageRules)
        {
            if (fieldDescriptor == null)
            {
                throw new ArgumentNullException(nameof(fieldDescriptor));
            }
            if (fieldRules.Ignore == Ignore.Unspecified)
            {
                // Note that adding a field to a `oneof` will also set the IfZeroValue on the fields. This means
                // only the field that os set will be validated and the unset fields are not validated according to the field rules.
                if (messageRules.Oneof.Any(messageRule => messageRule.Fields.Any(oneOfFieldName => string.Equals(fieldDescriptor.Name, oneOfFieldName, StringComparison.Ordinal))))
                {
                    return Ignore.IfZeroValue;
                }

                if (fieldDescriptor.HasPresence)
                {
                    if (fieldDescriptor.ContainingType == null || !fieldDescriptor.ContainingType.IsMapEntry)
                    {
                        return Ignore.IfZeroValue;
                    }
                }
            }
            return fieldRules.Ignore;
        }
    }
}
