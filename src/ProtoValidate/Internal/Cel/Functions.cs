// Copyright 2024-2025 TELUS
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

using Cel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ProtoValidate.Internal.Cel;

internal static class Functions
{
    public static void RegisterProtoValidateFunctions(this CelEnvironment celEnvironment)
    {
        celEnvironment.RegisterFunction("isNan", new[] { typeof(double) }, IsNan);
        celEnvironment.RegisterFunction("isNan", new[] { typeof(float) }, IsNan);
        celEnvironment.RegisterFunction("isInf", new[] { typeof(double) }, IsInf);
        celEnvironment.RegisterFunction("isInf", new[] { typeof(float) }, IsInf);

        celEnvironment.RegisterFunction("isEmail", new[] { typeof(string) }, IsEmail);
        celEnvironment.RegisterFunction("isHostname", new[] { typeof(string) }, IsHostname);
        celEnvironment.RegisterFunction("isHostAndPort", new[] { typeof(string), typeof(bool) }, IsHostAndPort);
        celEnvironment.RegisterFunction("isIp", new[] { typeof(string) }, IsIP);
        celEnvironment.RegisterFunction("isIp", new[] { typeof(string), typeof(int) }, IsIP);
        celEnvironment.RegisterFunction("isIp", new[] { typeof(string), typeof(long) }, IsIP);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string) }, IsIPPrefix);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(bool) }, IsIPPrefix);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(int) }, IsIPPrefixWithVersion);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(int), typeof(bool) }, IsIPPrefixWithVersion);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(long) }, IsIPPrefixWithVersion);
        celEnvironment.RegisterFunction("isIpPrefix", new[] { typeof(string), typeof(long), typeof(bool) }, IsIPPrefixWithVersion);
        celEnvironment.RegisterFunction("isUri", new[] { typeof(string) }, IsUri);
        celEnvironment.RegisterFunction("isUriRef", new[] { typeof(string) }, IsUriRef);

        celEnvironment.RegisterFunction("unique", new[] { typeof(IEnumerable) }, Unique);

        celEnvironment.RegisterFunction("startsWith", new[] { typeof(ByteString), typeof(ByteString) }, StartsWith_Bytes);
        celEnvironment.RegisterFunction("endsWith", new[] { typeof(ByteString), typeof(ByteString) }, EndsWith_Bytes);
        celEnvironment.RegisterFunction("contains", new[] { typeof(ByteString), typeof(ByteString) }, Contains_Bytes);

        celEnvironment.RegisterFunction("getField", new[] { typeof(IMessage), typeof(string) }, GetField);
    }

    internal static object? GetField(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new CelExpressionParserException("GetField function requires 2 arguments.");
        }

        if (!(args[0] is IMessage message))
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'getField' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }

        if (!(args[1] is string fieldName))
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'getField' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }

        if (message == null)
        {
            throw new CelExpressionParserException("GetField function argument 1 cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new CelExpressionParserException("GetField function argument 2 cannot be null.");
        }


        var fieldDescriptor = message.Descriptor.FindFieldByName(fieldName);
        if (fieldDescriptor == null)
        {
            throw new CelExpressionParserException($"GetField function cannot find field '{fieldName}'.");
        }

        return fieldDescriptor.Accessor.GetValue(message);
    }


    internal static object? Unique(object?[] args)
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

    internal static object? IsNan(object?[] args)
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

    internal static object? IsInf(object?[] args)
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

        throw new CelNoSuchOverloadException(
            $"No overload exists to for 'isInf' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    internal static object? IsIP(object?[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            throw new CelExpressionParserException("IsIp function requires 1 or 2 arguments.");
        }

        int? version = null;
        if (args.Length == 2)
        {
            if (args[1] is long versionLong)
            {
                if (versionLong == 0)
                {
                    version = null;
                }
                else if (versionLong == 4)
                {
                    version = 4;
                }
                else if (versionLong == 6)
                {
                    version = 6;
                }
                else
                {
                    return false;
                }
            }
            else if (args[1] is int versionInt)
            {
                if (versionInt == 0)
                {
                    version = null;
                }
                else if (versionInt == 4)
                {
                    version = 4;
                }
                else if (versionInt == 6)
                {
                    version = 6;
                }
                else
                {
                    return false;
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

    internal static bool IsIP(string value, long? version)
    {
        if (value.StartsWith("[", StringComparison.Ordinal))
        {
            return false;
        }

        var isIpV4 = TryParseStrictIPv4(value, out _);
        var isIpV6 = TryParseStrictIpv6(value, out _, out _);

        if (isIpV4)
        {
            return version == 4 || version.GetValueOrDefault() == 0;
        }

        if (isIpV6)
        {
            return version == 6 || version.GetValueOrDefault() == 0;
        }

        return false;
    }

    internal static object? IsIPPrefix(object?[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            throw new CelExpressionParserException("IsIpPrefix function requires 1 or 2 arguments.");
        }

        var validNetworkAddress = false;
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

    internal static object? IsIPPrefixWithVersion(object?[] args)
    {
        if (args.Length != 2 && args.Length != 3)
        {
            throw new CelExpressionParserException("IsIpPrefix function requires 2 or 3 arguments.");
        }

        int? version;
        var validNetworkAddress = false;

        if (args[1] is long versionLong)
        {
            if (versionLong == 0)
            {
                version = null;
            }
            else if (versionLong == 4)
            {
                version = 4;
            }
            else if (versionLong == 6)
            {
                version = 6;
            }
            else
            {
                return false;
            }
        }
        else if (args[1] is int versionInt)
        {
            if (versionInt == 0)
            {
                version = null;
            }
            else if (versionInt == 4)
            {
                version = 4;
            }
            else if (versionInt == 6)
            {
                version = 6;
            }
            else
            {
                return false;
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

        throw new CelNoSuchOverloadException(
            $"No overload exists to for 'isIpPrefix' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    internal static bool IsIPNetworkWithVersion(string value, long? version, bool checkIfValidNetworkAddress)
    {
        var parts = value.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        var isIpV4 = TryParseStrictIPv4(parts[0], out var ipv4Address);
        var isIpV6 = TryParseStrictIpv6(parts[0], out var ipv6Address, out var ipv6ZoneId);


        if (!isIpV4 && !isIpV6)
        {
            return false;
        }

        var address = isIpV4 ? ipv4Address! : ipv6Address!;

        if (isIpV4 && version.GetValueOrDefault() != 0 && version != 4)
        {
            return false;
        }

        if (isIpV6 && version.GetValueOrDefault() != 0 && version != 6)
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mask))
        {
            return false;
        }

        if (!string.Equals(parts[1], mask.ToString("0"), StringComparison.Ordinal))
        {
            return false;
        }

        if (isIpV4 && (mask < 0 || mask > 32))
        {
            return false;
        }

        if (isIpV6 && (mask < 0 || mask > 128))
        {
            return false;
        }

        if (ipv6ZoneId != null)
        {
            return false;
        }

        if (checkIfValidNetworkAddress)
        {
            var addressBytes = address.GetAddressBytes();
            var maskLengthInBits = addressBytes.Length * 8;

            var maskBitArray = new BitArray(maskLengthInBits);
            for (var i = 0; i < maskLengthInBits; i++)
            {
                //Index calculation is a bit strange, since you have to make your mind about byte order.
                var index = (int)((maskLengthInBits - i - 1) / 8) * 8 + i % 8;

                if (i < maskLengthInBits - mask)
                {
                    maskBitArray.Set(index, false);
                }
                else
                {
                    maskBitArray.Set(index, true);
                }
            }

            var addressBitArray = new BitArray(addressBytes);

            for (var i = 0; i < maskLengthInBits; i++)
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

    internal static object? IsEmail(object?[] args)
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

            // Check for any whitespace characters (including newlines, carriage returns, tabs, etc.)
            if (valueString.Any(char.IsWhiteSpace))
            {
                return false;
            }

            var parts = valueString.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            // Local part (before @) cannot be longer than 64 characters per RFC 5321
            // if (parts[0].Length > 64)
            // {
            //     return false;
            // }

            if (!Regex.IsMatch(valueString, @"\A[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*\z"))
            {
                return false;
            }

            return true;
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isEmail' function with argument type '{value?.GetType().FullName ?? "null"}'.");
    }

    internal static object? IsHostname(object?[] args)
    {
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

    internal static bool IsHostname(string value)
    {
        if (value.Length > 253)
        {
            return false;
        }

        string str;
        if (value.EndsWith("."))
        {
            str = value.Substring(0, value.Length - 1);
        }
        else
        {
            str = value;
        }

        var allDigits = false;

        var parts = str.Split('.');

        // split hostname on '.' and validate each part
        foreach (var part in parts)
        {
            allDigits = true;

            // if part is empty, longer than 63 chars, or starts/ends with '-', it is
            // invalid
            var len = part.Length;
            if (len == 0 || len > 63 || part.StartsWith("-") || part.EndsWith("-"))
            {
                return false;
            }

            // for each character in part
            for (var i = 0; i < part.Length; i++)
            {
                var c = part[i];
                // if the character is not a-z, A-Z, 0-9, or '-', it is invalid
                if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '-')
                {
                    return false;
                }

                allDigits = allDigits && c >= '0' && c <= '9';
            }
        }

        // the last part cannot be all numbers
        return !allDigits;
    }

    internal static object? IsHostAndPort(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new CelExpressionParserException("The `isHostAndPort` function requires 2 arguments.");
        }

        var valueArg = args[0];
        var portRequiredArg = args[1];

        if (valueArg is string valueString && portRequiredArg is bool portRequired)
        {
            return IsHostAndPort(valueString, portRequired);
        }

        throw new CelNoSuchOverloadException($"No overload exists to for 'isHostAndPort' function with argument types '{valueArg?.GetType().FullName ?? "null"}' and '{portRequiredArg?.GetType().FullName ?? "null"}'.");
    }

    internal static bool IsHostAndPort(string value, bool portRequired)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var isHostNameInBrackets = false;
        var host = value;
        var port = string.Empty;

        var indexOfOpenBrackets = value.IndexOf('[');
        var lastIndexOfCloseBrackets = value.LastIndexOf(']');
        if (indexOfOpenBrackets == 0 && lastIndexOfCloseBrackets > 0)
        {
            //we have an ipv6 address.
            host = value.Substring(1, lastIndexOfCloseBrackets - 1);
            isHostNameInBrackets = true;
        }

        if (lastIndexOfCloseBrackets < value.Length - 1 && value[lastIndexOfCloseBrackets + 1] == ':')
        {
            //two characters here, one for the ] and one for the :
            port = value.Substring(lastIndexOfCloseBrackets + 2);
        }
        else
        {
            var splitIdx = value.LastIndexOf(':');
            if (splitIdx > 0 && splitIdx < value.Length - 1 && splitIdx > lastIndexOfCloseBrackets)
            {
                host = value.Substring(0, splitIdx);
                port = value.Substring(splitIdx + 1);
            }
        }

        if (string.IsNullOrWhiteSpace(port))
        {
            if (isHostNameInBrackets)
            {
                return !portRequired && IsIP(host, 6);
            }

            return !portRequired && (IsHostname(host) || IsIP(host, 4));
        }

        if (isHostNameInBrackets)
        {
            return IsIP(host, 6) && ValidatePort(port);
        }

        return (IsHostname(host) || IsIP(host, 4)) && ValidatePort(port);
    }


    internal static object? IsUri(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsUri function requires 1 argument.");
        }

        if (args[0] is not string valueString)
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'isUri' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }

        return IsValidUri(valueString);
    }

    internal static object? IsUriRef(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new CelExpressionParserException("IsUriRef function requires 1 argument.");
        }

        if (args[0] is not string valueString)
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'isUriRef' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }

        return IsValidUriReference(valueString);
    }

    #region URI Validation Methods

    /// <summary>
    ///     Validates if a string is a valid absolute URI according to RFC 3986
    /// </summary>
    private static bool IsValidUri(string uriString)
    {
        if (string.IsNullOrEmpty(uriString))
        {
            return false;
        }

        // URIs starting with "//" are relative references, not absolute URIs
        if (uriString.StartsWith("//"))
        {
            return false;
        }

        try
        {
            const string regexPattern = @"^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?";

            var match = Regex.Match(uriString, regexPattern);
            if (!match.Success)
            {
                return false;
            }

            var scheme = match.Groups.Count >= 3 ? match.Groups[2].Value : string.Empty;
            var authority = match.Groups.Count >= 5 ? match.Groups[4].Value : string.Empty;
            var path = match.Groups.Count >= 6 ? match.Groups[5].Value : string.Empty;
            var query = match.Groups.Count >= 8 ? match.Groups[7].Value : string.Empty;
            var fragment = match.Groups.Count >= 10 ? match.Groups[9].Value : string.Empty;

            // Absolute URIs must have a scheme
            if (string.IsNullOrEmpty(scheme))
            {
                return false;
            }

            // Check for Unicode characters first - RFC 3986 requires ASCII only
            if (ContainsNonAsciiCharacters(uriString))
            {
                return false;
            }

            // Check for obviously invalid patterns first, even before .NET validation
            if (!IsValidPercentEncoding(uriString))
            {
                return false;
            }

            // Check for invalid unescaped characters that should be percent-encoded
            if (ContainsInvalidUnescapedCharacters(uriString))
            {
                return false;
            }

            // Check for invalid syntax patterns
            if (ContainsInvalidSyntaxPatterns(uriString))
            {
                return false;
            }

            // Validate scheme
            if (!IsValidScheme(scheme))
            {
                return false;
            }

            // Validate authority if present (authority is present if we have "//")
            var hasAuthority = match.Groups.Count >= 4 && match.Groups[3].Value.StartsWith("//");
            if (hasAuthority)
            {
                if (!ValidateAuthority(authority))
                {
                    return false;
                }
            }

            // Validate path component
            if (!string.IsNullOrEmpty(path))
            {
                if (!ValidateUriComponent(path, false)) // path doesn't allow '?' or '#'
                {
                    return false;
                }
            }

            // Validate query component
            if (!string.IsNullOrEmpty(query))
            {
                if (!ValidateUriComponent(query, true)) // query allows most characters
                {
                    return false;
                }
            }

            // Validate fragment component
            if (!string.IsNullOrEmpty(fragment))
            {
                if (!ValidateUriComponent(fragment, true)) // fragment allows most characters
                {
                    return false;
                }
            }

            // If all our manual checks pass, the URI is valid according to RFC 3986
            // We don't need to rely on .NET's parser since it's more restrictive
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Validates if a string is a valid URI reference according to RFC 3986
    ///     URI references include both absolute URIs and relative references
    /// </summary>
    private static bool IsValidUriReference(string uriRefString)
    {
        if (string.IsNullOrEmpty(uriRefString))
        {
            return true;
        }

        try
        {
            // Check for invalid percent encoding first
            if (!IsValidPercentEncoding(uriRefString))
            {
                return false;
            }

            // Check for invalid unescaped characters that should be percent-encoded
            if (ContainsInvalidUnescapedCharacters(uriRefString))
            {
                return false;
            }

            // Check for invalid syntax patterns
            if (ContainsInvalidSyntaxPatterns(uriRefString))
            {
                return false;
            }

            // First try as absolute URI using our enhanced validation
            if (IsValidUri(uriRefString))
            {
                return true;
            }

            // Then try as relative reference using .NET's built-in parser
            if (Uri.TryCreate(uriRefString, UriKind.Relative, out _))
            {
                // Additional validation for relative references that contain colons
                // RFC 3986: first segment of relative-path cannot contain colon
                if (!uriRefString.StartsWith("/") && !uriRefString.StartsWith("?") && !uriRefString.StartsWith("#"))
                {
                    var firstSlashIndex = uriRefString.IndexOf('/');
                    var firstQuestionIndex = uriRefString.IndexOf('?');
                    var firstHashIndex = uriRefString.IndexOf('#');

                    // Find the end of the first segment
                    var firstSegmentEnd = uriRefString.Length;
                    if (firstSlashIndex >= 0)
                    {
                        firstSegmentEnd = Math.Min(firstSegmentEnd, firstSlashIndex);
                    }

                    if (firstQuestionIndex >= 0)
                    {
                        firstSegmentEnd = Math.Min(firstSegmentEnd, firstQuestionIndex);
                    }

                    if (firstHashIndex >= 0)
                    {
                        firstSegmentEnd = Math.Min(firstSegmentEnd, firstHashIndex);
                    }

                    var firstSegment = uriRefString.Substring(0, firstSegmentEnd);

                    // If first segment contains colon, this might be misidentified as a URI with invalid scheme
                    if (firstSegment.Contains(":"))
                    {
                        var colonIndex = firstSegment.IndexOf(':');
                        var possibleScheme = firstSegment.Substring(0, colonIndex);

                        // If the part before colon looks like an invalid scheme, reject it
                        if (!IsValidScheme(possibleScheme))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Checks if a string contains any non-ASCII characters
    ///     RFC 3986 requires that URIs use only ASCII characters
    /// </summary>
    private static bool ContainsNonAsciiCharacters(string input)
    {
        foreach (var c in input)
        {
            if (c > 127) // ASCII characters are 0-127
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Validates percent encoding in a URI string
    /// </summary>
    private static bool IsValidPercentEncoding(string uriString)
    {
        for (var i = 0; i < uriString.Length; i++)
        {
            if (uriString[i] == '%')
            {
                // Must have at least 2 more characters
                if (i + 2 >= uriString.Length)
                {
                    return false;
                }

                // Next two characters must be hex digits
                var hex1 = uriString[i + 1];
                var hex2 = uriString[i + 2];

                if (!IsHexDigit(hex1) || !IsHexDigit(hex2))
                {
                    return false;
                }

                i += 2; // Skip the hex digits
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if a URI contains invalid unescaped characters that should be percent-encoded
    ///     According to RFC 3986, certain characters must be percent-encoded in URIs
    /// </summary>
    private static bool ContainsInvalidUnescapedCharacters(string uriString)
    {
        // Characters that are not allowed unescaped in URIs according to RFC 3986
        // These are characters outside the "unreserved" and "reserved" sets that .NET might accept
        var invalidChars = new[] { '^', '`', '{', '}', '|', '\\', '"' , '<', '>', ' ', '\t', '\r', '\n' };

        for (var i = 0; i < uriString.Length; i++)
        {
            var c = uriString[i];

            // Check for obviously invalid characters
            if (invalidChars.Contains(c))
            {
                return true;
            }

            // Check for control characters (ASCII 0-31 except allowed ones)
            if (c < 32 && c != '\t') // Most control characters are invalid
            {
                return true;
            }

            // Check for Unicode escape sequences like \u001f
            if (c == '\\' && i + 5 < uriString.Length &&
                uriString[i + 1] == 'u' &&
                IsHexDigit(uriString[i + 2]) && IsHexDigit(uriString[i + 3]) &&
                IsHexDigit(uriString[i + 4]) && IsHexDigit(uriString[i + 5]))
            {
                // This looks like a Unicode escape sequence, which should not appear in URIs
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Checks for invalid URI syntax patterns that .NET might accept but violate RFC 3986
    /// </summary>
    private static bool ContainsInvalidSyntaxPatterns(string uriString)
    {
        // Check for double hash (##) which is invalid syntax
        if (uriString.Contains("##"))
        {
            return true;
        }

        // Check for double question marks (??) which is invalid syntax
        if (uriString.Contains("??"))
        {
            return true;
        }

        // Check for multiple consecutive colons in scheme (after the first colon)
        var firstColonIndex = uriString.IndexOf(':');
        if (firstColonIndex > 0 && firstColonIndex < uriString.Length - 1)
        {
            // Look for patterns like "http:::/" which are invalid
            var afterScheme = uriString.Substring(firstColonIndex + 1);
            if (afterScheme.StartsWith("::") && !afterScheme.StartsWith("://"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Validates a URI scheme according to RFC 3986
    /// </summary>
    private static bool IsValidScheme(string scheme)
    {
        if (string.IsNullOrEmpty(scheme))
        {
            return false;
        }

        // First character must be ALPHA
        if (!char.IsLetter(scheme[0]))
        {
            return false;
        }

        // Remaining characters must be ALPHA / DIGIT / "+" / "-" / "."
        for (var i = 1; i < scheme.Length; i++)
        {
            var c = scheme[i];
            if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Validates a URI authority component (userinfo@host:port)
    /// </summary>
    private static bool ValidateAuthority(string authority)
    {
        if (string.IsNullOrEmpty(authority))
        {
            return true; // Empty authority is valid (e.g., "file:///path")
        }

        // Split userinfo from host:port
        var atIndex = authority.LastIndexOf('@');
        var hostPort = atIndex >= 0 ? authority.Substring(atIndex + 1) : authority;

        // Validate userinfo if present
        if (atIndex >= 0)
        {
            var userInfo = authority.Substring(0, atIndex);
            if (!ValidateUserInfo(userInfo))
            {
                return false;
            }
        }

        // Parse host and port
        string host;
        var port = "";

        if (hostPort.StartsWith("["))
        {
            // IPv6 literal
            var closeBracket = hostPort.IndexOf(']');
            if (closeBracket <= 1) // Must have content and close bracket
            {
                return false;
            }

            host = hostPort.Substring(1, closeBracket - 1);

            // Validate IPv6 address
            if (!ValidateIPv6Host(host))
            {
                return false;
            }

            // Check for port after closing bracket
            if (closeBracket < hostPort.Length - 1)
            {
                if (hostPort[closeBracket + 1] != ':')
                {
                    return false; // Invalid character after ]
                }

                if (closeBracket + 2 < hostPort.Length)
                {
                    port = hostPort.Substring(closeBracket + 2);
                }
            }
        }
        else
        {
            // Regular host (hostname or IPv4)
            var colonIndex = hostPort.LastIndexOf(':');
            if (colonIndex >= 0 && colonIndex < hostPort.Length - 1)
            {
                host = hostPort.Substring(0, colonIndex);
                port = hostPort.Substring(colonIndex + 1);
            }
            else if (colonIndex == hostPort.Length - 1)
            {
                // Trailing colon with no port
                host = hostPort.Substring(0, colonIndex);
            }
            else
            {
                host = hostPort;
            }

            // Validate host (can be hostname or IPv4) - allow empty host
            if (!ValidateHost(host))
            {
                return false;
            }
        }

        // Validate port if present
        if (!string.IsNullOrEmpty(port))
        {
            if (!ValidateUriAuthorityPort(port))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Validates userinfo component
    /// </summary>
    private static bool ValidateUserInfo(string userInfo)
    {
        // Empty userinfo is valid (like in "https://@example.com")
        if (string.IsNullOrEmpty(userInfo))
        {
            return true;
        }

        // userinfo = *( unreserved / pct-encoded / sub-delims / ":" )
        // RFC 3986 allows all sub-delims including "?" in userinfo
        for (var i = 0; i < userInfo.Length; i++)
        {
            var c = userInfo[i];

            if (c == '%')
            {
                // Must be followed by two hex digits
                if (i + 2 >= userInfo.Length ||
                    !IsHexDigit(userInfo[i + 1]) ||
                    !IsHexDigit(userInfo[i + 2]))
                {
                    return false;
                }

                i += 2;
            }
            else if (!char.IsLetterOrDigit(c) &&
                     c != '-' && c != '.' && c != '_' && c != '~' &&
                     c != '!' && c != '$' && c != '&' && c != '\'' &&
                     c != '(' && c != ')' && c != '*' && c != '+' &&
                     c != ',' && c != ';' && c != '=' && c != ':' && c != '?' && c != '#')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Validates an IPv6 host component
    /// </summary>
    private static bool ValidateIPv6Host(string host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return false; // IPv6 literal cannot be empty
        }

        // Check for zone ID (scope ID) indicated by %
        var percentIndex = host.IndexOf('%');
        var ipPart = percentIndex >= 0 ? host.Substring(0, percentIndex) : host;
        var zoneId = percentIndex >= 0 ? host.Substring(percentIndex + 1) : null;

        // For IPv6 literals, we use our strict parsing method
        if (!TryParseStrictIpv6(ipPart, out _, out _))
        {
            // If it doesn't parse as IPv6, check if it's a valid IPv6 future literal
            // IPv6 future format: v<HEXDIG>+.<unreserved / sub-delims / ":" / "/">
            if (host.StartsWith("v") && host.Length > 1)
            {
                var dotIndex = host.IndexOf('.');
                if (dotIndex > 1)
                {
                    var versionPart = host.Substring(1, dotIndex - 1);
                    var addressPart = host.Substring(dotIndex + 1);

                    // Version part must be all hex digits
                    foreach (var c in versionPart)
                    {
                        if (!IsHexDigit(c))
                        {
                            return false;
                        }
                    }

                    // Address part can contain unreserved / sub-delims / ":" / "/"
                    foreach (var c in addressPart)
                    {
                        if (!char.IsLetterOrDigit(c) &&
                            c != '-' && c != '.' && c != '_' && c != '~' &&
                            c != '!' && c != '$' && c != '&' && c != '\'' &&
                            c != '(' && c != ')' && c != '*' && c != '+' &&
                            c != ',' && c != ';' && c != '=' && c != ':' && c != '/')
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        // Validate zone ID if present
        if (zoneId != null)
        {
            // Special case: %25 is URL-encoded %, which results in an empty zone ID
            // This is invalid according to RFC 3986
            if (zoneId == "25")
            {
                return false;
            }

            if (string.IsNullOrEmpty(zoneId))
            {
                return false;
            }

            // Validate percent encoding in zone ID
            if (zoneId.Contains("%") && !IsValidZoneIdPercentEncoding(zoneId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Validates a host component (hostname or IPv4 address)
    /// </summary>
    private static bool ValidateHost(string host)
    {
        // Empty host is valid in some contexts (like "https://:8080")
        if (string.IsNullOrEmpty(host))
        {
            return true;
        }

        // Validate percent encoding in host more strictly
        if (!IsValidHostPercentEncoding(host))
        {
            return false;
        }

        // Check for invalid characters first
        foreach (var c in host)
        {
            // RFC 3986: host = IP-literal / IPv4address / reg-name
            // reg-name = *( unreserved / pct-encoded / sub-delims )
            // unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
            // sub-delims = "!" / "$" / "&" / "'" / "(" / ")" / "*" / "+" / "," / ";" / "="

            if (!char.IsLetterOrDigit(c) &&
                c != '-' && c != '.' && c != '_' && c != '~' &&
                c != '!' && c != '$' && c != '&' && c != '\'' &&
                c != '(' && c != ')' && c != '*' && c != '+' &&
                c != ',' && c != ';' && c != '=' && c != '%')
            {
                return false;
            }
        }

        // Try as IPv4 first - if it fails, treat as hostname
        if (TryParseStrictIPv4(host, out _))
        {
            return true;
        }

        // If it looks like IPv4 but fails strict parsing, treat as hostname anyway
        // This handles cases like "256.0.0.1" which should be valid hostnames
        return IsHostname(host) || IsValidRegName(host);
    }

    /// <summary>
    ///     Validates a reg-name according to RFC 3986 (more permissive than hostname)
    /// </summary>
    private static bool IsValidRegName(string regName)
    {
        if (string.IsNullOrEmpty(regName))
        {
            return true;
        }

        // reg-name can contain any unreserved / pct-encoded / sub-delims characters
        // This is more permissive than hostname rules
        for (var i = 0; i < regName.Length; i++)
        {
            var c = regName[i];

            if (c == '%')
            {
                // Must be followed by two hex digits
                if (i + 2 >= regName.Length ||
                    !IsHexDigit(regName[i + 1]) ||
                    !IsHexDigit(regName[i + 2]))
                {
                    return false;
                }

                i += 2;
            }
            else if (!char.IsLetterOrDigit(c) &&
                     c != '-' && c != '.' && c != '_' && c != '~' &&
                     c != '!' && c != '$' && c != '&' && c != '\'' &&
                     c != '(' && c != ')' && c != '*' && c != '+' &&
                     c != ',' && c != ';' && c != '=' && c != ':')
            {
                return false;
            }
        }

        return true;
    }


    /// <summary>
    ///     Validates a port number for a uri authority
    /// </summary>
    private static bool ValidatePort(string port)
    {
        if (string.IsNullOrEmpty(port))
        {
            return false;
        }


        if (int.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var portNum))
        {
            if (!string.Equals(port, portNum.ToString("0", CultureInfo.InvariantCulture)))
            {
                //don't allow double zero port.
                return false;
            }

            if (portNum >= 0 && portNum <= 65535)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Validates a port number for a uri authority
    /// </summary>
    private static bool ValidateUriAuthorityPort(string port)
    {
        if (string.IsNullOrEmpty(port))
        {
            return true; // Empty port is valid in authority (just the colon)
        }

        // Port must be all digits
        foreach (var c in port)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        // Convert to number and check range
        if (int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var portNumber))
        {
            return portNumber >= 0;
        }

        return false;
    }



    /// <summary>
    ///     Validates a URI component (path, query, or fragment)
    /// </summary>
    private static bool ValidateUriComponent(string component, bool allowQuery)
    {
        if (string.IsNullOrEmpty(component))
        {
            return true;
        }

        for (var i = 0; i < component.Length; i++)
        {
            var c = component[i];

            if (c == '%')
            {
                // Must be followed by two hex digits
                if (i + 2 >= component.Length ||
                    !IsHexDigit(component[i + 1]) ||
                    !IsHexDigit(component[i + 2]))
                {
                    return false;
                }

                i += 2;
            }
            else if (!IsValidUriChar(c, allowQuery))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if a character is valid in a URI component
    /// </summary>
    private static bool IsValidUriChar(char c, bool allowQuery)
    {
        // unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
        if (char.IsLetterOrDigit(c) || c == '-' || c == '.' || c == '_' || c == '~')
        {
            return true;
        }

        // sub-delims = "!" / "$" / "&" / "'" / "(" / ")" / "*" / "+" / "," / ";" / "="
        if (c == '!' || c == '$' || c == '&' || c == '\'' ||
            c == '(' || c == ')' || c == '*' || c == '+' ||
            c == ',' || c == ';' || c == '=' )
        {
            return true;
        }

        // Additional allowed characters in paths, queries, fragments
        if (c == '/' || c == ':' || c == '@')
        {
            return true;
        }

        // Query and fragment specific characters
        if (allowQuery && (c == '?' || c == '#'))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if character is a hexadecimal digit.
    /// </summary>
    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
    }

    #endregion

    internal static object StartsWith_Bytes(object?[] args)
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

    internal static object EndsWith_Bytes(object?[] args)
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

    internal static object Contains_Bytes(object?[] args)
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

    public static bool TryParseStrictIPv4(string ipString, out IPAddress? ipAddress)
    {
        // 1. First, use the built-in TryParse.
        // This handles general IP parsing and validation of octet values.
        if (!IPAddress.TryParse(ipString, out ipAddress))
        {
            return false;
        }

        // 2. Check that the parsed address is specifically an IPv4 address.
        if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        // 3. Perform a strict check for four dotted-decimal parts.
        // The built-in parser allows shorthand notations (e.g., "127.1" becomes "127.0.0.1"),
        // so we must compare the string representation to the original.
        return ipAddress.ToString() == ipString;
    }

    /// <summary>
    ///     Strictly parses a string as an IPv6 address.
    ///     This method accepts addresses with zone identifiers (e.g., "fe80::1%eth0") while maintaining strict parsing.
    ///     It fails if the address uses IPv4-mapped notation or other non-standard formats.
    /// </summary>
    /// <param name="addressString">The string to parse.</param>
    /// <param name="address">When this method returns, contains the parsed IPAddress if successful.</param>
    /// <param name="zoneId">When this method returns, contains the parsed ipv6 zone if successful.</param>
    /// <returns>True if the parsing was strict, otherwise false.</returns>
    public static bool TryParseStrictIpv6(string addressString, out IPAddress? address, out string? zoneId)
    {
        address = null;
        zoneId = null;

        // Handle zone identifier (scope ID) by splitting on '%' character
        string ipPart;

        var zoneIndex = addressString.IndexOf('%');
        if (zoneIndex >= 0)
        {
            ipPart = addressString.Substring(0, zoneIndex);
            zoneId = addressString.Substring(zoneIndex + 1);

            // Validate zone identifier - must be non-empty
            // Zone IDs can contain various characters including spaces and control characters
            if (string.IsNullOrEmpty(zoneId))
            {
                return false;
            }

            // Only validate percent encoding in zone ID if it appears to use percent encoding
            // (i.e., contains % followed by what looks like hex digits)
            if (zoneId.Contains("%") && HasPercentEncodingPattern(zoneId) && !IsValidZoneIdPercentEncoding(zoneId))
            {
                return false;
            }
        }
        else
        {
            ipPart = addressString;
        }

        // 1. First, use the standard TryParse to check for basic validity of the IP part.
        if (!IPAddress.TryParse(ipPart, out var tempAddress))
        {
            return false;
        }

        // 2. The IPAddress object must represent an IPv6 address.
        if (tempAddress.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        // 3. Check if the parsed address represents the same IPv6 address by comparing
        //    the canonical form with a re-parsed version of the canonical form.
        //    This ensures we accept valid IPv6 formats including those with leading zeros.
        var canonicalForm = tempAddress.ToString();
        if (IPAddress.TryParse(canonicalForm, out var canonicalAddress) && tempAddress.Equals(canonicalAddress))
        {
            // Additional validation: ensure the input doesn't contain invalid characters or formats
            // by checking that when we parse it again, we get the same address
            if (IPAddress.TryParse(ipPart, out var reparsedOriginal) && tempAddress.Equals(reparsedOriginal))
            {
                address = tempAddress;
                return true;
            }
        }

        // At this point, the string parsed to an IPv6 address, but it wasn't
        // in a valid format that we can safely accept.
        return false;
    }

    /// <summary>
    ///     Validates percent encoding specifically in host components with stricter rules
    /// </summary>
    private static bool IsValidHostPercentEncoding(string host)
    {
        for (var i = 0; i < host.Length; i++)
        {
            if (host[i] == '%')
            {
                // Must have at least 2 more characters
                if (i + 2 >= host.Length)
                {
                    return false;
                }

                // Next two characters must be hex digits
                var hex1 = host[i + 1];
                var hex2 = host[i + 2];

                if (!IsHexDigit(hex1) || !IsHexDigit(hex2))
                {
                    return false;
                }

                // For host components, be stricter about malformed percent encoding patterns
                // If there's a 3rd character that's a letter but not hex, it looks like malformed encoding
                if (i + 3 < host.Length)
                {
                    var possibleHex3 = host[i + 3];
                    // If the character after a valid %XX is a letter (but not hex), this looks like
                    // malformed percent encoding like %c3x, %a5g, etc.
                    if (char.IsLetter(possibleHex3) && !IsHexDigit(possibleHex3))
                    {
                        return false;
                    }
                }

                i += 2; // Skip the hex digits
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if a zone ID string contains patterns that look like percent encoding
    /// </summary>
    private static bool HasPercentEncodingPattern(string zoneId)
    {
        for (var i = 0; i < zoneId.Length - 2; i++)
        {
            if (zoneId[i] == '%')
            {
                // Check if next two characters look like hex digits
                var char1 = zoneId[i + 1];
                var char2 = zoneId[i + 2];
                
                if (IsHexDigit(char1) && IsHexDigit(char2))
                {
                    return true; // Found a pattern that looks like percent encoding
                }
            }
        }
        return false; // No percent encoding patterns found
    }

    /// <summary>
    ///     Validates percent encoding specifically in IPv6 zone IDs with stricter rules
    /// </summary>
    private static bool IsValidZoneIdPercentEncoding(string zoneId)
    {
        for (var i = 0; i < zoneId.Length; i++)
        {
            if (zoneId[i] == '%')
            {
                // Must have at least 2 more characters
                if (i + 2 >= zoneId.Length)
                {
                    return false;
                }

                // Next two characters must be hex digits
                var hex1 = zoneId[i + 1];
                var hex2 = zoneId[i + 2];

                if (!IsHexDigit(hex1) || !IsHexDigit(hex2))
                {
                    return false;
                }

                // For zone IDs, be stricter about malformed percent encoding patterns
                // If there's a 3rd character that's a letter but not hex, it looks like malformed encoding
                if (i + 3 < zoneId.Length)
                {
                    var possibleHex3 = zoneId[i + 3];
                    // If the character after a valid %XX is a letter (but not hex), this looks like
                    // malformed percent encoding like %c3x, %a5g, etc.
                    if (char.IsLetter(possibleHex3) && !IsHexDigit(possibleHex3))
                    {
                        return false;
                    }
                }

                i += 2; // Skip the hex digits
            }
        }

        return true;
    }
}