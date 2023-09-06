using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate;

public interface IValidator
{
    bool FailFast { get; set; }
    bool DisableLazy { get; set; }
    ValidationResult Validate(IMessage message);
    void Initialize(IEnumerable<FileDescriptor> fileDescriptors);
    string GetEvaluatorDebugString(IMessage message);
}