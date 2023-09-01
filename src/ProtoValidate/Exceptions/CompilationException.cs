namespace ProtoValidate.Exceptions;

public class CompilationException : ValidationException
{
    public CompilationException() { }
    public CompilationException(string? message) : base(message) { }
    public CompilationException(string? message, Exception? innerException) : base(message, innerException) { }
}