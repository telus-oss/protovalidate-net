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

using System.Collections;
using System.Globalization;
using System.Text;
using Cel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace ProtoValidate.Internal.Cel;

internal static class FormatFunction
{
    internal static readonly char[] HEX_ARRAY = "0123456789ABCDEF".ToCharArray();
    internal static readonly char[] LOWER_HEX_ARRAY = "0123456789abcdef".ToCharArray();

    public static void RegisterProtoValidateFormatFunction(this CelEnvironment celEnvironment)
    {
        celEnvironment.RegisterFunction("format", new[] { typeof(string), typeof(object?[]) }, Format);
    }

    internal static object? Format(object?[] args)
    {
        if (args.Length < 2)
        {
            throw new CelExpressionParserException("Format function requires 2 arguments.");
        }

        if (args[0] is not string fmtString)
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'format' function with argument type '{args[0]?.GetType().FullName ?? "null"}'.");
        }

        if (args[1] is not object?[] formatArgs)
        {
            throw new CelNoSuchOverloadException($"No overload exists to for 'format' function with argument type '{args[1]?.GetType().FullName ?? "null"}'.");
        }

        // StringBuilder to accumulate the formatted string
        var builder = new StringBuilder();
        var index = 0;
        var argIndex = 0;
        while (index < fmtString.Length)
        {
            var c = fmtString[index++];
            if (c != '%')
            {
                // Append non-format characters directly
                builder.Append(c);
                // Add the entire character if it's not a UTF-8 character.
                if ((c & 0x80) != 0)
                {
                    // Add the rest of the UTF-8 character.
                    while (index < fmtString.Length && (fmtString[index] & 0xc0) == 0x80)
                    {
                        builder.Append(fmtString[index++]);
                    }
                }

                continue;
            }

            if (index >= fmtString.Length)
            {
                throw new CelExpressionParserException("format: expected format specifier");
            }

            if (fmtString[index] == '%')
            {
                // Escaped '%', Append '%' and move to the next character
                builder.Append('%');
                index++;
                continue;
            }

            if (argIndex >= formatArgs.Length)
            {
                throw new CelExpressionParserException("format: not enough arguments");
            }

            var arg = formatArgs[argIndex++];
            c = fmtString[index++];

            if (c == '.')
            {
                // parse the precision
                var precision = 0;
                while (index < fmtString.Length && '0' <= fmtString[index] && fmtString[index] <= '9')
                {
                    precision = precision * 10 + (fmtString[index++] - '0');
                }

                if (index >= fmtString.Length)
                {
                    throw new CelExpressionParserException("format: expected format specifier");
                }

                c = fmtString[index++];
            }

            switch (c)
            {
                case 'd':
                    builder.Append(FormatStringSafe(arg, false));
                    break;
                case 'x':
                    builder.Append(FormatHex(arg, LOWER_HEX_ARRAY));
                    break;
                case 'X':
                    builder.Append(FormatHex(arg, HEX_ARRAY));
                    break;
                case 's':
                    builder.Append(FormatString(arg));
                    break;
                case 'e':
                case 'f':
                case 'b':
                case 'o':
                default:
                    throw new CelExpressionParserException($"format: unparsable format specifier {c}");
            }
        }

        return builder.ToString();
    }

    /**
   * Formats a string value.
   *
   * @param builder the StringBuilder to append the formatted string to.
   * @param val the value to format.
   * @return the formatted string value.
   */
    internal static string FormatString(object? val)
    {
        if (val is string valString)
        {
            return valString;
        }

        return FormatStringSafe(val, false);
    }

    internal static string FormatHex(object? val)
    {
        if (val is string valString)
        {
            return valString;
        }

        return FormatStringSafe(val, false);
    }

    /**
   * Formats a string value safely for other value types.
   *
   * @param builder the StringBuilder to append the formatted string to.
   * @param val the value to format.
   * @param listType indicates if the value type is a list.
   * @return the formatted string value.
   */
    internal static string FormatStringSafe(object? val, bool listType)
    {
        if (val is bool valBool)
        {
            return valBool.ToString().ToLowerInvariant();
        }

        if (val is int valInt)
        {
            return FormatInt(valInt);
        }

        if (val is uint valUInt)
        {
            return FormatUInt(valUInt);
        }

        if (val is long valLong)
        {
            return FormatInt(valLong);
        }

        if (val is ulong valULong)
        {
            return FormatUInt(valULong);
        }

        if (val is double valDouble)
        {
            return FormatDouble(valDouble);
        }

        if (val is float valFloat)
        {
            return FormatDouble(valFloat);
        }

        if (val is string valString)
        {
            return valString;
        }

        if (val is ByteString valByteString)
        {
            return FormatBytes(valByteString);
        }

        if (val is Duration valDuration)
        {
            return FormatDuration(valDuration, listType);
        }

        if (val is Timestamp valTimestamp)
        {
            return FormatTimestamp(valTimestamp);
        }

        if (val is IList valIList)
        {
            return FormatList(valIList);
        }

        throw new CelExpressionParserException("format: unimplemented stringSafe type");
    }

    internal static string FormatBytes(ByteString val)
    {
        return val.ToStringUtf8();
    }

    internal static string FormatInt(long val)
    {
        return val.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatUInt(ulong val)
    {
        return val.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatDouble(double val)
    {
        return val.ToString("0.#######################", CultureInfo.InvariantCulture);
    }

    internal static string FormatTimestamp(Timestamp value)
    {
        var ticks = TimeSpan.FromTicks(value.Nanos / 100);
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(value.Seconds).Add(ticks);

        string serializedValue;
        if (dateTimeOffset.Millisecond == 0)
        {
            serializedValue = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);
        }
        else
        {
            serializedValue = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss'.'fffK", CultureInfo.InvariantCulture);
        }

        var timestampString = serializedValue.Replace("+00:00", "Z");

        return timestampString;
    }

    internal static string FormatDuration(Duration value, bool listType)
    {
        var ticks = value.Seconds * 1000000000 + value.Nanos;
        var totalSeconds = ticks / 1000000000d;

        var durationString = totalSeconds.ToString("0.###########", CultureInfo.InvariantCulture) + "s";

        return durationString;
    }

    internal static string FormatList(IList val)
    {
        var sb = new StringBuilder();
        sb.Append('[');

        for (var i = 0; i < val.Count; i++)
        {
            sb.Append(FormatStringSafe(val[i], true));
            if (i != val.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(']');

        return sb.ToString();
    }

    internal static string BytesToHex(byte[] bytes, char[] digits)
    {
        var hexChars = new char[bytes.Length * 2];
        for (var j = 0; j < bytes.Length; j++)
        {
            var v = bytes[j] & 0xFF;
            hexChars[j * 2] = digits[v >> 4];
            hexChars[j * 2 + 1] = digits[v & 0x0F];
        }

        return new string(hexChars);
    }

    internal static string FormatHex(object? val, char[] digits)
    {
        if (val is int valInt)
        {
            var bytes = BitConverter.GetBytes(valInt);
            return BytesToHex(bytes, digits);
        }

        if (val is long valLong)
        {
            var bytes = BitConverter.GetBytes(valLong);
            return BytesToHex(bytes, digits);
        }

        if (val is uint valUInt)
        {
            var bytes = BitConverter.GetBytes(valUInt);
            return BytesToHex(bytes, digits);
        }

        if (val is ulong valULong)
        {
            var bytes = BitConverter.GetBytes(valULong);
            return BytesToHex(bytes, digits);
        }

        if (val is ByteString valByteString)
        {
            return BytesToHex(valByteString.ToByteArray(), digits);
        }

        if (val is string valString)
        {
            return valString;
        }

        throw new CelExpressionParserException("formatHex: expected int or string");
    }
}