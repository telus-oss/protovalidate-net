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

using System.Globalization;
using System.Reflection;
using Buf.Validate;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal;

internal static class FieldDescriptorExtensions
{
    internal static readonly PropertyInfo ProtoPropertyInfo = typeof(FieldDescriptor).GetProperty("Proto", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static object? GetDefaultValue(this FieldDescriptor fieldDescriptor)
    {
        var fieldDescriptorProto = (FieldDescriptorProto)ProtoPropertyInfo!.GetValue(fieldDescriptor)!;
        var defaultValue = fieldDescriptorProto.DefaultValue;


        if (fieldDescriptor.FieldType == FieldType.String)
        {
            if (defaultValue == null && fieldDescriptor.HasPresence)
            {
                if (fieldDescriptor.ContainingType.IsMapEntry)
                {
                    return string.Empty;
                }

                return null;
            }

            return defaultValue;
        }

        if (fieldDescriptor.FieldType == FieldType.Bool)
        {
            if (bool.TryParse(defaultValue, out var defaultBool))
            {
                return defaultBool;
            }

            return false;
        }

        if (fieldDescriptor.FieldType == FieldType.Int32
            || fieldDescriptor.FieldType == FieldType.Enum
            || fieldDescriptor.FieldType == FieldType.SFixed32
            || fieldDescriptor.FieldType == FieldType.SFixed64
            || fieldDescriptor.FieldType == FieldType.Int64
            || fieldDescriptor.FieldType == FieldType.SInt32
            || fieldDescriptor.FieldType == FieldType.SInt64)
        {
            if (long.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var defaultLong))
            {
                return defaultLong;
            }

            return 0;
        }

        if (fieldDescriptor.FieldType == FieldType.UInt32
            || fieldDescriptor.FieldType == FieldType.UInt64
            || fieldDescriptor.FieldType == FieldType.Fixed32
            || fieldDescriptor.FieldType == FieldType.Fixed64
           )
        {
            if (ulong.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var defaultUlong))
            {
                return defaultUlong;
            }

            return 0u;
        }

        if (fieldDescriptor.FieldType == FieldType.Double || fieldDescriptor.FieldType == FieldType.Float)
        {
            if (double.TryParse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var defaultDouble))
            {
                return defaultDouble;
            }

            return 0.0;
        }

        if (fieldDescriptor.FieldType == FieldType.Bytes)
        {
            return ByteString.Empty;
        }

        return null;
    }
}