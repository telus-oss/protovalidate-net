using ProtoValidate.Internal.Cel;

namespace ProtoValidate.Exceptions;

public class ExecutionException : ValidationException
{
    public Expression? SourceExpression { get; } 
    public ExecutionException() { }
    public ExecutionException(string? message) : base(message) { }

    public ExecutionException(string? message, Expression sourceExpression) : base(message)
    {
        SourceExpression = sourceExpression;
    }

    public ExecutionException(string? message, Exception? innerException, Expression sourceExpression) : base(message, innerException)
    {
        SourceExpression = sourceExpression;
    }

    public ExecutionException(string? message, Exception? innerException) : base(message, innerException) { }
}