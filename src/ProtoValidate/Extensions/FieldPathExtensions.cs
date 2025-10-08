using System.Text;
using Buf.Validate;

namespace ProtoValidate;

public static class FieldPathExtensions
{
    public static string GetPath(this FieldPath fieldPath)
    {
        if (fieldPath == null)
        {
            throw new ArgumentNullException(nameof(fieldPath));
        }

        var builder = new StringBuilder();
        foreach (var element in fieldPath.Elements)
        {
            if (builder.Length > 0)
            {
                builder.Append(".");
            }

            builder.Append(element.FieldName);

            // Handle subscript cases
            if (element.HasIndex)
            {
                builder.Append("[");
                builder.Append(element.Index);
                builder.Append("]");
            }
            else if (element.HasBoolKey)
            {
                if (element.BoolKey)
                {
                    builder.Append("[true]");
                }
                else
                {
                    builder.Append("[false]");
                }
            }
            else if (element.HasIntKey)
            {
                builder.Append("[");
                builder.Append(element.IntKey);
                builder.Append("]");
            }
            else if (element.HasUintKey)
            {
                builder.Append("[");
                builder.Append(element.UintKey);
                builder.Append("]");
            }
            else if (element.HasStringKey)
            {
                builder.Append("[\"");
                builder.Append(element.StringKey.Replace("\\", "\\\\").Replace("\"", "\\\""));
                builder.Append("\"]");
            }
        }

        return builder.ToString();
    }
}