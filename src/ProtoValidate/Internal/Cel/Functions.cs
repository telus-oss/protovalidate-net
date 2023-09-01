using System.Net;
using System.Net.Sockets;
using Cel;
using Cel.Internal;

namespace ProtoValidate.Internal.Cel;

public static class Functions
{
    public static void RegisterFunctions(IDictionary<string, CelFunctionDelegate> dict)
    {
        dict.Add("isNan", IsNan);
        dict.Add("isInf", IsInf);
        dict.Add("isEmail", IsEmail);
        dict.Add("isHostname", IsHostname);
        dict.Add("isIp", IsIP);
        dict.Add("isUri", IsUri);
        dict.Add("isUriRef", IsUriRef);
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

            var parts = valueString.Split("@");
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

        var split = lowerCaseHostname.Split(".");


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


        return Uri.TryCreate(valueString, UriKind.Absolute, out var uri);
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
}