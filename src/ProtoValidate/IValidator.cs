using Google.Protobuf;

namespace ProtoValidate;

public interface IValidator
{
    ValidationResult Validate(IMessage message, bool failFast);
    string GetEvaluatorDebugString(IMessage message);
}