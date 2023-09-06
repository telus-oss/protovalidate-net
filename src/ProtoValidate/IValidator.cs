using Google.Protobuf;

namespace ProtoValidate;

public interface IValidator
{
    ValidationResult Validate(IMessage message);
    string GetEvaluatorDebugString(IMessage message);
}