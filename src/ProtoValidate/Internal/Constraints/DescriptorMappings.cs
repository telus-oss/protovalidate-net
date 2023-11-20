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

using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Constraints;

public class DescriptorMappings
{
    // Provides a {@link Descriptor} for {@link FieldConstraints}.
    private static MessageDescriptor FIELD_CONSTRAINTS_DESC { get; } = FieldConstraints.Descriptor;

    // Provides the {@link OneofDescriptor} for the type union in {@link FieldConstraints}.
    internal static OneofDescriptor FIELD_CONSTRAINTS_ONEOF_DESC { get; } = FIELD_CONSTRAINTS_DESC.Oneofs[0];

    // Provides the {@link FieldDescriptor} for the map standard constraints.
    private static FieldDescriptor MAP_FIELD_CONSTRAINTS_DESC { get; } = FIELD_CONSTRAINTS_DESC.FindFieldByName("map");

    // Provides the {@link FieldDescriptor} for the repeated standard constraints.
    private static FieldDescriptor REPEATED_FIELD_CONSTRAINTS_DESC { get; } = FIELD_CONSTRAINTS_DESC.FindFieldByName("repeated");

    // Maps protocol buffer field kinds to their expected field constraints.
    private static Dictionary<FieldType, FieldDescriptor> EXPECTED_STANDARD_CONSTRAINTS { get; } = new();

    // Returns the {@link build.buf.validate.FieldConstraints} field that is expected for the given
    // wrapper well-known type's full name. If ok is false, no standard constraints exist for that
    // type.   
    private static Dictionary<string, FieldDescriptor> EXPECTED_WKT_CONSTRAINTS { get; } = new();

    static DescriptorMappings()
    {
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Float, FIELD_CONSTRAINTS_DESC.FindFieldByName("float"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Double, FIELD_CONSTRAINTS_DESC.FindFieldByName("double"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Int32, FIELD_CONSTRAINTS_DESC.FindFieldByName("int32"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Int64, FIELD_CONSTRAINTS_DESC.FindFieldByName("int64"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.UInt32, FIELD_CONSTRAINTS_DESC.FindFieldByName("uint32"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.UInt64, FIELD_CONSTRAINTS_DESC.FindFieldByName("uint64"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.SInt32, FIELD_CONSTRAINTS_DESC.FindFieldByName("sint32"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.SInt64, FIELD_CONSTRAINTS_DESC.FindFieldByName("sint64"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Fixed32, FIELD_CONSTRAINTS_DESC.FindFieldByName("fixed32"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Fixed64, FIELD_CONSTRAINTS_DESC.FindFieldByName("fixed64"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.SFixed32, FIELD_CONSTRAINTS_DESC.FindFieldByName("sfixed32"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.SFixed64, FIELD_CONSTRAINTS_DESC.FindFieldByName("sfixed64"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Bool, FIELD_CONSTRAINTS_DESC.FindFieldByName("bool"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.String, FIELD_CONSTRAINTS_DESC.FindFieldByName("string"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Bytes, FIELD_CONSTRAINTS_DESC.FindFieldByName("bytes"));
        EXPECTED_STANDARD_CONSTRAINTS.Add(FieldType.Enum, FIELD_CONSTRAINTS_DESC.FindFieldByName("enum"));
        EXPECTED_WKT_CONSTRAINTS.Add("google.protobuf.Any", FIELD_CONSTRAINTS_DESC.FindFieldByName("any"));
        EXPECTED_WKT_CONSTRAINTS.Add("google.protobuf.Duration", FIELD_CONSTRAINTS_DESC.FindFieldByName("duration"));
        EXPECTED_WKT_CONSTRAINTS.Add("google.protobuf.Timestamp", FIELD_CONSTRAINTS_DESC.FindFieldByName("timestamp"));
    }


    /**
   * Returns the {@link FieldConstraints} field that is expected for the given protocol buffer field
   * kind.
   */
    public static FieldDescriptor? ExpectedWrapperConstraints(string fqn)
    {
        switch (fqn)
        {
            case "google.protobuf.BoolValue":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.Bool];
            case "google.protobuf.BytesValue":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.Bytes];
            case "google.protobuf.DoubleValue":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.Double];
            case "google.protobuf.FloatValue":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.Float];
            case "google.protobuf.Int32Value":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.Int32];
            case "google.protobuf.Int64Value":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.Int64];
            case "google.protobuf.StringValue":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.String];
            case "google.protobuf.UInt32Value":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.UInt32];
            case "google.protobuf.UInt64Value":
                return EXPECTED_STANDARD_CONSTRAINTS[FieldType.UInt64];
            default:
                return null;
        }
    }

    /**
   * Maps a {@link FieldType} to a compatible {@link com.google.api.expr.v1alpha1.Type}.
   */
    // public static Type protoKindToCELType(FieldType kind) {
    //     switch (kind) {
    //         case FLOAT:
    //         case DOUBLE:
    //             return Decls.newPrimitiveType(Type.PrimitiveType.DOUBLE);
    //         case INT32:
    //         case INT64:
    //         case SINT32:
    //         case SINT64:
    //         case SFIXED32:
    //         case SFIXED64:
    //         case ENUM:
    //             return Decls.newPrimitiveType(Type.PrimitiveType.INT64);
    //         case UINT32:
    //         case UINT64:
    //         case FIXED32:
    //         case FIXED64:
    //             return Decls.newPrimitiveType(Type.PrimitiveType.UINT64);
    //         case BOOL:
    //             return Decls.newPrimitiveType(Type.PrimitiveType.BOOL);
    //         case STRING:
    //             return Decls.newPrimitiveType(Type.PrimitiveType.STRING);
    //         case BYTES:
    //             return Decls.newPrimitiveType(Type.PrimitiveType.BYTES);
    //         case MESSAGE:
    //         case GROUP:
    //             return Type.newBuilder().setMessageType(kind.getJavaType().name()).build();
    //         default:
    //             return Type.newBuilder()
    //                 .setPrimitive(Type.PrimitiveType.PRIMITIVE_TYPE_UNSPECIFIED)
    //                 .build();
    //     }
    // }
    public static FieldDescriptor? GetExpectedConstraintDescriptor(FieldDescriptor fieldDescriptor, bool forItems)
    {
        if (fieldDescriptor.IsMap)
        {
            return MAP_FIELD_CONSTRAINTS_DESC;
        }

        if (fieldDescriptor.IsRepeated && !forItems)
        {
            return REPEATED_FIELD_CONSTRAINTS_DESC;
        }

        if (fieldDescriptor.FieldType == FieldType.Message)
        {
            if (EXPECTED_WKT_CONSTRAINTS.TryGetValue(fieldDescriptor.MessageType.FullName, out var wellKnownType))
            {
                return wellKnownType;
            }

            return null;
        }

        return EXPECTED_STANDARD_CONSTRAINTS[fieldDescriptor.FieldType];
    }
}