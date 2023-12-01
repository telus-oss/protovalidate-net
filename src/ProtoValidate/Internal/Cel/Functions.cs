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

using System.Collections;
using System.Net;
using System.Net.Sockets;
using Cel;
using Google.Protobuf;

namespace ProtoValidate.Internal.Cel;

public static class Functions
{
    public static void RegisterProtoValidateFunctions(this CelEnvironment celEnvironment)
    {
        celEnvironment.RegisterFunction("isNan", new[] { typeof(double) }, IsNan);
        celEnvironment.RegisterFunction("isNan", new[] { typeof(float) }, IsNan);
        celEnvironment.RegisterFunction("isInf", new[] { typeof(double) }, IsInf);
        celEnvironment.RegisterFunction("isInf", new[] { typeof(float) }, IsInf);

        celEnvironment.RegisterFunction("isEmail", new[] { typeof(string) }, IsEmail);
        celEnvironment.RegisterFunction("isHostname", new[] { typeof(string) }, IsHostname);
        celEnvironment.RegisterFunction("isIp", new[] { typeof(string) }, IsIP);
        celEnvironment.RegisterFunction("isIp", new[] { typeof(string), typeof(long) }, IsIP);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string) }, IsIPPrefix);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(bool) }, IsIPPrefix);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(long) }, IsIPPrefixWithVersion);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(long), typeof(bool) }, IsIPPrefixWithVersion);
        celEnvironment.RegisterFunction("isUri", new[] { typeof(string) }, IsUri);
        celEnvironment.RegisterFunction("isUriRef", new[] { typeof(string) }, IsUriRef);

        celEnvironment.RegisterFunction("unique", new[] { typeof(IEnumerable) }, Unique);

        celEnvironment.RegisterFunction("startsWith", new[] { typeof(ByteString), typeof(ByteString) }, StartsWith_Bytes);
        celEnvironment.RegisterFunction("endsWith", new[] { typeof(ByteString), typeof(ByteString) }, EndsWith_Bytes);
        celEnvironment.RegisterFunction("contains", new[] { typeof(ByteString), typeof(ByteString) }, Contains_Bytes);
    }

    private static object? Unique(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("Unique function requires 1 argument.");
        }

        var value = args[0];

        if (value is IEnumerable valueList)
        {
            var list = valueList.Cast<object?>().ToList();
            return list.Count == list.Distinct().Count();
        }


        throw new CelNoSuchOverloadException($"No overload exists to for 'unique' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static object? IsNan(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsNan function requires 1 argument.");
        }

        var value = args[0];
        if (value is float valueFloat)
        {
            return float.IsNaN(valueFloat);
        }

        if (value is double valueDouble)
        {
            return double.IsNaN(valueDouble);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isNan' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static object? IsInf(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsInf function requires 1 argument.");
        }

        var value = args[0];
        if (value is float valueFloat)
        {
            return float.IsInfinity(valueFloat);
        }

        if (value is double valueDouble)
        {
            return double.IsInfinity(valueDouble);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isInf' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static object? IsIP(object?[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            throw new CelExpressionParserException("IsIp function requires 1 or 2 arguments.");
        }

        int? version = null;
        if (args.Length == 2)
        {
            if (args[1] is long versionInt)
            {
                if (versionInt == 4)
                {
                    version = 4;
                }
                else if (versionInt == 6)
                {
                    version = 6;
                }
                else
                {
                    throw new CelExpressionParserException("Invalid IP version number.");
                }
            }
            else
            {
                throw new CelNoSuchOverloadException($"No overload exists to for 'isIp' function with argument type '{args[1]?.GetType().FullName ?? "null"}'.");
            }
        }

        var value = args[0];
        if (value is string valueString)
        {
            return IsIP(valueString, version);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isIp' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static bool IsIP(string value, long? version)
    {
        if (IPAddress.TryParse(value, out var address))
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return !version.HasValue || version.Value == 4;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return !version.HasValue || version.Value == 6;
            }
        }

        return false;
    }

    private static object? IsIPPrefix(object?[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            throw new CelExpressionParserException("IsIpPrefix function requires 1 or 2 arguments.");
        }

        bool validNetworkAddress = false;
        if (args.Length == 2)
        {
            if (args[1] is bool boolValidNetworkAddress)
            {
                validNetworkAddress = boolValidNetworkAddress;
            }
            else
            {
                throw new CelNoSuchOverloadException($"No overload exists to for 'isIpPrefix' function with argument type '{args[1]?.GetType().FullName ?? "null"}'.");
            }
        }

        var value = args[0];
        if (value is string valueString)
        {
            return IsIPNetworkWithVersion(valueString, null, validNetworkAddress);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isIpPrefix' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static object? IsIPPrefixWithVersion(object?[] args)
    {
        if (args.Length != 2 && args.Length != 3)
        {
            throw new CelExpressionParserException("IsIpPrefix function requires 2 or 3 arguments.");
        }

        int? version;
        bool validNetworkAddress = false;

        if (args[1] is long versionInt)
        {
            if (versionInt == 4)
            {
                version = 4;
            }
            else if (versionInt == 6)
            {
                version = 6;
            }
            else
            {
                throw new CelExpressionParserException("Invalid IPPrefix version number.");
            }
        }
        else
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'isIpPrefix' function with argument type '{args[1]?.GetType().FullName ?? "null"}'.");
        }


        if (args.Length == 3)
        {
            if (args[2] is bool boolValidNetworkAddress)
            {
                validNetworkAddress = boolValidNetworkAddress;
            }
            else
            {
                throw new CelNoSuchOverloadException($"No overload exists to for 'isIpPrefix' function with argument type '{args[1]?.GetType().FullName ?? "null"}'.");
            }
        }


        var value = args[0];
        if (value is string valueString)
        {
            return IsIPNetworkWithVersion(valueString, version, validNetworkAddress);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isIpPrefix' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static bool IsIPNetworkWithVersion(string value, long? version, bool checkIfValidNetworkAddress)
    {
        string[] parts = value.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out var address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork && version.HasValue && version.Value != 4)
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6 && version.HasValue && version.Value != 6)
        {
            return false;
        }

        if (!int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var mask))
        {
            return false;
        }

        if (checkIfValidNetworkAddress)
        {
            var addressBytes = address.GetAddressBytes();
            int maskLengthInBits = addressBytes.Length * 8;

            BitArray maskBitArray = new BitArray(maskLengthInBits);
            for (int i = 0; i < maskLengthInBits; i++)
            {
                //Index calculation is a bit strange, since you have to make your mind about byte order.
                int index = (int)((maskLengthInBits - i - 1) / 8) * 8 + (i % 8);

                if (i < (maskLengthInBits - mask))
                {
                    maskBitArray.Set(index, false);
                }
                else
                {
                    maskBitArray.Set(index, true);
                }
            }

            var addressBitArray = new BitArray(addressBytes);

            for (int i = 0; i < maskLengthInBits; i++)
            {
                //if the mask bit is zero and the address bit is non-zero, then we don't have a network.
                if (!maskBitArray[i] && addressBitArray[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static object? IsEmail(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsEmail function requires 1 argument.");
        }

        var value = args[0];
        if (value is string valueString)
        {
            if (valueString.Contains("<"))
            {
                return false;
            }

            if (valueString.Length > 254)
            {
                return false;
            }

            var parts = valueString.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            return parts[0].Length < 64 && IsHostname(parts[1]);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isEmail' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static object? IsHostname(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsHostname function requires 1 argument.");
        }

        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsHostname function requires 1 argument.");
        }

        var value = args[0];
        if (value is string valueString)
        {
            return IsHostname(valueString);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isHostname' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    private static bool IsHostname(string value)
    {
        if (value.Length > 253)
        {
            return false;
        }

        if (value.Length == 0)
        {
            return true;
        }

        var lowerCaseHostname = value.ToLower();
        if (lowerCaseHostname.EndsWith("."))
        {
            lowerCaseHostname = lowerCaseHostname.Substring(0, lowerCaseHostname.Length - 1);
        }

        var split = lowerCaseHostname.Split('.');


        foreach (var part in split)
        {
            var len = part.Length;
            if (len == 0 || len > 63 || part[0] == '-' || part[len - 1] == '-')
            {
                return false;
            }

            for (var i = 0; i < part.Length; i++)
            {
                var ch = part[i];

                if ((ch < 'a' || ch > 'z') && (ch < '0' || ch > '9') && ch != '-')
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static object? IsUri(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsUri function requires 1 argument.");
        }

        if (args[0] is not string valueString)
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'isUri' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }


        var isUri =  Uri.TryCreate(valueString, UriKind.Absolute, out var uri);
        if (!isUri)
        {
            return false;
        }

        // this URI fragment "/foo/bar?baz=quux" returns false when we try to create on windows, but returns true on linux/macos with the prefix "file://"
        // so it looks like "file:///foo/bar?baz=quux" when we parse on linux and call uri.ToString();
        // since this isn't a full URI, we need this hack here to return a failure.
        // but we don't want full uri's that legitimately start with "file://" to get caught up in this exception.

        if (uri != null && uri.ToString().StartsWith("file://", StringComparison.Ordinal) && !valueString.StartsWith("file://", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static object? IsUriRef(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsUriRef function requires 1 argument.");
        }

        if (args[0] is not string valueString)
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'isUriRef' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }


        if (Uri.TryCreate(valueString, UriKind.Absolute, out var uri))
        {
            return true;
        }

        if (Uri.TryCreate("http://protovalidate.buf.build" + valueString, UriKind.Absolute, out var uriRef))
        {
            return true;
        }

        return false;
    }

    private static object StartsWith_Bytes(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new CelExpressionParserException("StartsWith function requires 2 arguments.");
        }

        if (args[0] is ByteString byteStringValue1 && args[1] is ByteString byteStringValue2)
        {
            if (byteStringValue2.Length > byteStringValue1.Length)
            {
                return false;
            }

            for (var i = 0; i < byteStringValue2.Length; i++)
            {
                if (byteStringValue1[i] != byteStringValue2[i])
                {
                    return false;
                }
            }

            return true;
        }

        throw new CelNoSuchOverloadException($"No overload exists for 'startsWith' function with argument types '{args[0]?.GetType().FullName ?? "null"}' and '{args[1]?.GetType().FullName ?? "null"}.");
    }

    private static object EndsWith_Bytes(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new CelExpressionParserException("EndsWith function requires 2 arguments.");
        }

        if (args[0] is ByteString byteStringValue1 && args[1] is ByteString byteStringValue2)
        {
            if (byteStringValue2.Length > byteStringValue1.Length)
            {
                return false;
            }

            var offset = byteStringValue1.Length - byteStringValue2.Length;

            for (var i = 0; i < byteStringValue2.Length; i++)
            {
                if (byteStringValue1[i + offset] != byteStringValue2[i])
                {
                    return false;
                }
            }

            return true;
        }

        throw new CelNoSuchOverloadException($"No overload exists for 'endsWith' function with argument types '{args[0]?.GetType().FullName ?? "null"}' and '{args[1]?.GetType().FullName ?? "null"}.");
    }

    private static object Contains_Bytes(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new CelExpressionParserException("EndsWith function requires 2 arguments.");
        }

        if (args[0] is ByteString byteStringValue1 && args[1] is ByteString byteStringValue2)
        {
            if (byteStringValue2.Length > byteStringValue1.Length)
            {
                return false;
            }

            // Two pointers to traverse the arrays
            int i = 0, j = 0;
            var n = byteStringValue1.Length;
            var m = byteStringValue2.Length;

            // Traverse both arrays simultaneously
            while (i < n && j < m)
            {
                // If element matches
                // increment both pointers
                if (byteStringValue1[i] == byteStringValue2[j])
                {
                    i++;
                    j++;

                    // If array B is completely
                    // traversed
                    if (j == m)
                    {
                        return true;
                    }
                }

                // If not,
                // increment i and reset j
                else
                {
                    i = i - j + 1;
                    j = 0;
                }
            }

            return false;
        }

        throw new CelNoSuchOverloadException($"No overload exists for 'endsWith' function with argument types '{args[0]?.GetType().FullName ?? "null"}' and '{args[1]?.GetType().FullName ?? "null"}.");
    }
}