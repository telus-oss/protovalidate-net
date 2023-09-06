using Google.Protobuf.Reflection;

namespace ProtoValidate;

public class ValidatorOptions
{
    public ValidatorOptions() { }

    public bool FailFast { get; set; }
    public bool DisableLazy { get; set; }
    public IList<FileDescriptor>? FileDescriptors { get; set; }
    public bool PreLoadDescriptors { get; set; }
}