using Buf.Validate;

namespace ProtoValidate.Internal.Cel;

public class Expression
{
    /// <summary>The id of the constraint</summary>
    public string Id { get; }

    /// <summary>The message of the constraint</summary>
    public string Message { get; }

    /// <summary>The CEL Expression of the constraint of the constraint</summary>
    public string ExpressionText { get; }


    /// <summary>
    ///     Constructs a new expression
    /// </summary>
    /// <param name="id">The id of the constraint</param>
    /// <param name="message">The message of the constraint</param>
    /// <param name="expressionText">The CEL Expression of the constraint of the constraint</param>
    public Expression(string id, string message, string expressionText)
    {
        Id = id;
        Message = message;
        ExpressionText = expressionText;
    }

    public Expression(Constraint constraint)
    {
        if (constraint == null)
        {
            throw new ArgumentNullException(nameof(constraint));
        }

        Id = constraint.Id;
        Message = constraint.Message;
        ExpressionText = constraint.Expression;
    }

    public Expression(Buf.Validate.Priv.Constraint constraint)
    {
        if (constraint == null)
        {
            throw new ArgumentNullException(nameof(constraint));
        }

        Id = constraint.Id;
        Message = constraint.Message;
        ExpressionText = constraint.Expression;
    }

    public static IEnumerable<Expression> FromPrivConstraints(IEnumerable<Buf.Validate.Priv.Constraint> constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        foreach (var constraint in constraints)
        {
            yield return new Expression(constraint);
        }
    }

    public static IEnumerable<Expression> FromConstraints(IEnumerable<Constraint> constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        foreach (var constraint in constraints)
        {
            yield return new Expression(constraint);
        }
    }
}