using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace ProtoValidate.Conformance;

public static class FileDescriptorUtil
{
    public static TypeRegistry GetTypeRegistry()
    {
        var localFileDescriptors = GetFileDescriptors().ToArray();
        return TypeRegistry.FromFiles(localFileDescriptors);
    }
    public static IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            if (type.IsClass && type.Name.EndsWith("Reflection"))
            {
                var propertyInfo = type.GetProperty("Descriptor", BindingFlags.Static | BindingFlags.Public);
                if (propertyInfo != null)
                {
                    if (propertyInfo.PropertyType == typeof(FileDescriptor))
                    {
                        var fileDescriptor = (FileDescriptor?)propertyInfo.GetValue(null);
                        if (fileDescriptor != null)
                        {
                            yield return fileDescriptor;
                        }
                    }
                }
            }
        }

        yield return AnyReflection.Descriptor;
        yield return ApiReflection.Descriptor;
        yield return DurationReflection.Descriptor;
        yield return EmptyReflection.Descriptor;
        yield return FieldMaskReflection.Descriptor;
        yield return SourceContextReflection.Descriptor;
        yield return StructReflection.Descriptor;
        yield return TimestampReflection.Descriptor;
        yield return TypeReflection.Descriptor;
        yield return WrappersReflection.Descriptor;
    }

    public static Dictionary<string, MessageDescriptor> Parse(FileDescriptorSet fileDescriptorSet)
    {
        var descriptorDictionary = new Dictionary<string, MessageDescriptor>();
        var fileDescriptorDictionary = ParseFileDescriptors(fileDescriptorSet);
        foreach (var fileDescriptor in fileDescriptorDictionary.Values)
        {
            foreach (var messageType in fileDescriptor.MessageTypes)
            {
                descriptorDictionary[messageType.FullName] = messageType;
            }
        }

        return descriptorDictionary;
    }

    public static Dictionary<string, FileDescriptor> ParseFileDescriptors(FileDescriptorSet fileDescriptorSet)
    {
        var fileDescriptorProtoDictionary = new Dictionary<string, FileDescriptorProto>();
        foreach (var fileDescriptorProto in fileDescriptorSet.File)
        {
            if (fileDescriptorProtoDictionary.ContainsKey(fileDescriptorProto.Name))
            {
                throw new Exception("Duplicate files found.");
            }

            fileDescriptorProtoDictionary[fileDescriptorProto.Name] = fileDescriptorProto;
        }

        var fileDescriptorDictionary = new Dictionary<string, FileDescriptor>();
        foreach (var fileDescriptorProto in fileDescriptorSet.File)
        {
            if (fileDescriptorProto.Dependency.Count == 0)
            {
                fileDescriptorDictionary[fileDescriptorProto.Name] = FileDescriptor.FromGeneratedCode(fileDescriptorProto.ToByteArray(), new FileDescriptor[] { }, null);
                continue;
            }

            var dependencies = new List<FileDescriptor>();
            foreach (var dependency in fileDescriptorProto.Dependency)
            {
                if (fileDescriptorDictionary.TryGetValue(dependency, out var dependencyValue))
                {
                    dependencies.Add(dependencyValue);
                }
            }

            fileDescriptorDictionary[fileDescriptorProto.Name] = FileDescriptor.FromGeneratedCode(fileDescriptorProto.ToByteArray(), dependencies.ToArray(), null);
        }

        return fileDescriptorDictionary;
    }
}