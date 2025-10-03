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

namespace ProtoValidate.Internal.Rules;

internal class DescriptorMappings
{
    // Provides a {@link Descriptor} for {@link FieldRules}.
    internal static MessageDescriptor FIELD_RULES_DESC { get; } = FieldRules.Descriptor;

    // Provides the {@link OneofDescriptor} for the type union in {@link FieldRules}.
    internal static OneofDescriptor FIELD_RULES_ONEOF_DESC { get; } = FIELD_RULES_DESC.Oneofs[0];

    // Provides the {@link FieldDescriptor} for the map standard rules.
    internal static FieldDescriptor MAP_FIELD_RULES_DESC { get; } = FIELD_RULES_DESC.FindFieldByName("map");

    // Provides the {@link FieldDescriptor} for the repeated standard rules.
    internal static FieldDescriptor REPEATED_FIELD_RULES_DESC { get; } = FIELD_RULES_DESC.FindFieldByName("repeated");

    // Maps protocol buffer field kinds to their expected field rules.
    internal static Dictionary<FieldType, FieldDescriptor> EXPECTED_STANDARD_RULES { get; } = new();

    // Returns the {@link build.buf.validate.FieldRules} field that is expected for the given
    // wrapper well-known type's full name. If ok is false, no standard rules exist for that
    // type.   
    internal static Dictionary<string, FieldDescriptor> EXPECTED_WKT_RULES { get; } = new();

    static DescriptorMappings()
    {
        EXPECTED_STANDARD_RULES.Add(FieldType.Float, FIELD_RULES_DESC.FindFieldByName("float"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Double, FIELD_RULES_DESC.FindFieldByName("double"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Int32, FIELD_RULES_DESC.FindFieldByName("int32"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Int64, FIELD_RULES_DESC.FindFieldByName("int64"));
        EXPECTED_STANDARD_RULES.Add(FieldType.UInt32, FIELD_RULES_DESC.FindFieldByName("uint32"));
        EXPECTED_STANDARD_RULES.Add(FieldType.UInt64, FIELD_RULES_DESC.FindFieldByName("uint64"));
        EXPECTED_STANDARD_RULES.Add(FieldType.SInt32, FIELD_RULES_DESC.FindFieldByName("sint32"));
        EXPECTED_STANDARD_RULES.Add(FieldType.SInt64, FIELD_RULES_DESC.FindFieldByName("sint64"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Fixed32, FIELD_RULES_DESC.FindFieldByName("fixed32"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Fixed64, FIELD_RULES_DESC.FindFieldByName("fixed64"));
        EXPECTED_STANDARD_RULES.Add(FieldType.SFixed32, FIELD_RULES_DESC.FindFieldByName("sfixed32"));
        EXPECTED_STANDARD_RULES.Add(FieldType.SFixed64, FIELD_RULES_DESC.FindFieldByName("sfixed64"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Bool, FIELD_RULES_DESC.FindFieldByName("bool"));
        EXPECTED_STANDARD_RULES.Add(FieldType.String, FIELD_RULES_DESC.FindFieldByName("string"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Bytes, FIELD_RULES_DESC.FindFieldByName("bytes"));
        EXPECTED_STANDARD_RULES.Add(FieldType.Enum, FIELD_RULES_DESC.FindFieldByName("enum"));
        EXPECTED_WKT_RULES.Add("google.protobuf.Any", FIELD_RULES_DESC.FindFieldByName("any"));
        EXPECTED_WKT_RULES.Add("google.protobuf.Duration", FIELD_RULES_DESC.FindFieldByName("duration"));
        EXPECTED_WKT_RULES.Add("google.protobuf.Timestamp", FIELD_RULES_DESC.FindFieldByName("timestamp"));
    }


    /**
   * Returns the {@link FieldRules} field that is expected for the given protocol buffer field
   * kind.
   */
    public static FieldDescriptor? ExpectedWrapperRules(string fqn)
    {
        switch (fqn)
        {
            case "google.protobuf.BoolValue":
                return EXPECTED_STANDARD_RULES[FieldType.Bool];
            case "google.protobuf.BytesValue":
                return EXPECTED_STANDARD_RULES[FieldType.Bytes];
            case "google.protobuf.DoubleValue":
                return EXPECTED_STANDARD_RULES[FieldType.Double];
            case "google.protobuf.FloatValue":
                return EXPECTED_STANDARD_RULES[FieldType.Float];
            case "google.protobuf.Int32Value":
                return EXPECTED_STANDARD_RULES[FieldType.Int32];
            case "google.protobuf.Int64Value":
                return EXPECTED_STANDARD_RULES[FieldType.Int64];
            case "google.protobuf.StringValue":
                return EXPECTED_STANDARD_RULES[FieldType.String];
            case "google.protobuf.UInt32Value":
                return EXPECTED_STANDARD_RULES[FieldType.UInt32];
            case "google.protobuf.UInt64Value":
                return EXPECTED_STANDARD_RULES[FieldType.UInt64];
            default:
                return null;
        }
    }

    public static FieldDescriptor? GetExpectedRuleDescriptor(FieldDescriptor fieldDescriptor, bool forItems)
    {
        if (fieldDescriptor.IsMap)
        {
            return MAP_FIELD_RULES_DESC;
        }

        if (fieldDescriptor.IsRepeated && !forItems)
        {
            return REPEATED_FIELD_RULES_DESC;
        }

        if (fieldDescriptor.FieldType == FieldType.Message)
        {
            if (EXPECTED_WKT_RULES.TryGetValue(fieldDescriptor.MessageType.FullName, out var wellKnownType))
            {
                return wellKnownType;
            }

            return null;
        }

        return EXPECTED_STANDARD_RULES[fieldDescriptor.FieldType];
    }
}