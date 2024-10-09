using System.Reflection;
using System.Runtime.Serialization;
using Buf.Validate;
using Buf.Validate.Conformance.Cases;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ProtoValidate.Internal.Constraints;
using Type = Google.Protobuf.WellKnownTypes.Type;

namespace ProtoValidate.Tests;

[TestFixture]
public class PredefinedMessageTest
{
    [Test]
    [TestCase(2u, true)]
    [TestCase(3u, false)]
    public void TestExtensionRegistry(ulong input, bool shouldPass)
    {
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { PredefinedReflection.Descriptor, Buf.Validate.Conformance.Cases.IgnoreProtoEditionsReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false,
        };

        var validator = new Validator(validatorOptions);

        var message = new TestUInt64EvenValidatorMessage();
        message.Val = input;

        var validationResult = validator.Validate(message, false);

        Assert.That(validationResult.IsSuccess, Is.EqualTo(shouldPass));
    }

    [Test]
    public void TestExtensionRegistryOld()
    {
        //
        // var extensionRegistry = new ExtensionRegistry
        // {
        //     ValidateExtensions.Message,
        //     ValidateExtensions.Oneof,
        //     ValidateExtensions.Field,
        //     ValidateExtensions.Predefined,
        //     Buf.Validate.PredefinedExtensions.
        // };
        //
        //
        // var descriptorSerializedDataList = GetRequiredFileDescriptors().Union(new[] { PredefinedReflection.Descriptor }).Select(proto => proto.SerializedData).ToList();
        //
        // var typeRegistryFileDescriptors = FileDescriptor.BuildFromByteStrings(descriptorSerializedDataList, extensionRegistry).ToList();
        // var typeRegistry = TypeRegistry.FromFiles(typeRegistryFileDescriptors);
        //
        //
        // var messageDescriptor = TestUInt64EvenValidatorMessage.Descriptor;
        // var fullMessageDescriptor = typeRegistry.Find(TestUInt64EvenValidatorMessage.Descriptor.FullName);
        // //Assert.That(messageDescriptor, Is.EqualTo(fullMessageDescriptor));
        //
        // var fieldDescriptor = messageDescriptor.FindFieldByNumber(1);
        // var fullFieldDescriptor = fullMessageDescriptor.FindFieldByNumber(1);
        // //Assert.That(fieldDescriptor, Is.EqualTo(fullFieldDescriptor));
        //
        //
        // var fieldOptions = fieldDescriptor.GetOptions();
        // var fullFieldOptions = fullFieldDescriptor.GetOptions();
        // //Assert.That(fieldOptions, Is.EqualTo(fullFieldOptions));
        //
        //
        // var fieldConstraints = fieldOptions.GetExtension(ValidateExtensions.Field);
        // var fullFieldConstraints = fullFieldOptions.GetExtension(ValidateExtensions.Field);
        //
        //
        // var fieldConstraintDescriptor = ((IMessage)fieldConstraints).Descriptor;
        // var fullFieldConstraintDescriptor = typeRegistry.Find(((IMessage)fullFieldConstraints).Descriptor.FullName);
        //
        // var fieldConstraintOptions = fieldConstraintDescriptor.GetOptions();
        // var fullFieldConstraintOptions = fullFieldConstraintDescriptor.GetOptions();
        //
        // var fieldOneOfsDescriptor = fieldConstraintDescriptor.Oneofs[0];
        // var fullFieldOneOfsDescriptor = fullFieldConstraintDescriptor.Oneofs[0];
        //
        // var fieldOneOfsOptions = fieldOneOfsDescriptor.GetOptions();
        // var fullFieldOneOfsOptions = fullFieldOneOfsDescriptor.GetOptions();
        //
        // var oneofFieldDescriptor = fieldOneOfsDescriptor.Accessor.GetCaseFieldDescriptor(fieldConstraints);
        // //var fullOneofFieldDescriptor = fullFieldOneOfsDescriptor.Accessor.GetCaseFieldDescriptor(fullFieldConstraints);
        //
        // var expectedConstraintDescriptor = DescriptorMappings.GetExpectedConstraintDescriptor(fieldDescriptor, false);
        // var typedFieldConstraints = (IMessage)oneofFieldDescriptor.Accessor.GetValue(fieldConstraints);
        //
        // var typedFieldConstraintsDescriptor = ((IMessage)typedFieldConstraints).Descriptor;
        // var fullTypedFieldConstraintsDescriptor = typeRegistry.Find(((IMessage)typedFieldConstraints).Descriptor.FullName);
        //
        // var expectedConstraintDescriptorOptions = expectedConstraintDescriptor!.GetOptions();
        // var typedFieldConstraintsOptions = typedFieldConstraintsDescriptor.GetOptions();
        // var fullTypedFieldConstraintsOptions = fullTypedFieldConstraintsDescriptor.GetOptions();
        //
        //
        // // var extendableMessage = (IExtendableMessage<UInt64Rules>)typedFieldConstraints;
        // // var extensionValue = ((IExtendableMessage<UInt64Rules>)typedFieldConstraints).GetExtension(Buf.Validate.PredefinedExtensions.Uint64EvenTest);
        //
        //
        //
        // var constraintType = typedFieldConstraints.GetType();
        //
        // var extensionsPropertyInfo = constraintType.GetProperty("_Extensions", BindingFlags.Instance | BindingFlags.NonPublic);
        // var extensionSet = extensionsPropertyInfo!.GetValue(typedFieldConstraints);
        //
        // var extendableMessageType = typeof(IExtendableMessage<>).MakeGenericType(constraintType);
        //
        //
        //
        //
        // // var extensionSetType = typeof(ExtensionSet<>);
        // // var genericExtensionSetType = extensionSetType.MakeGenericType(constraintType);
        // // var valuesByNumberProperty = genericExtensionSetType.GetProperty("ValuesByNumber", BindingFlags.NonPublic|BindingFlags.Instance)!;
        // // var valuesByNumberDictionary = (Dictionary<int, IExtensionValue>)valuesByNumberProperty.GetValue(extensionSet);
        //
        //
        //
        //
        //
        // // internal Dictionary<int, IExtensionValue> ValuesByNumber { get; } = new Dictionary<int, IExtensionValue>();
        //
        //
        //
        //
        // foreach (var extension in extensionRegistry)
        // {
        //     var extensionType = extension.GetType();
        //     var extensionTypeGenericArguments = extensionType.GetGenericArguments();
        //
        //
        //     var targetType = GetExtensionTargetType(extension)!;
        //
        //     if (constraintType == targetType)
        //     {
        //         var valueType = extensionTypeGenericArguments[1];
        //
        //
        //         var hasExtensionMethod = extendableMessageType.GetMethod("HasExtension", BindingFlags.Instance | BindingFlags.Public);
        //         if (hasExtensionMethod == null)
        //         {
        //             throw new Exception("HasExtension method not found on type.");
        //         }
        //
        //         var genericHasExtensionMethod = hasExtensionMethod.MakeGenericMethod(new System.Type[] { valueType });
        //
        //         var hasExtension = (bool?)genericHasExtensionMethod.Invoke(typedFieldConstraints, new object?[] { extension }) ?? false;
        //         if (!hasExtension)
        //         {
        //             continue;
        //         }
        //
        //         var getExtensionValueMethod = extendableMessageType.GetMethods().FirstOrDefault(c => c.Name == "GetExtension" && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extensionType.GetGenericTypeDefinition());
        //         //var getExtensionValueMethod = extendableMessageType.GetMethod("GetExtension", BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, new System.Type[] { extensionType.GetGenericTypeDefinition() }, null);
        //         if (getExtensionValueMethod == null)
        //         {
        //             throw new Exception("GetExtension method not found on type.");
        //         }
        //
        //         var genericGetExtensionValueMethod = getExtensionValueMethod.MakeGenericMethod(valueType);
        //
        //
        //         var extensionValue = genericGetExtensionValueMethod.Invoke(typedFieldConstraints, new object?[] { extension });
        //
        //
        //         //TValue GetExtension<TValue>(Extension<T, TValue> extension);
        //         //RepeatedField<TValue> GetExtension<TValue>(RepeatedExtension<T, TValue> extension);
        //
        //
        //     }
        //
        //
        //
        // }
        //
        // //     private static bool TryGetValue<TTarget>(
        // //   ref ExtensionSet<TTarget> set,
        // //   Extension extension,
        // //   out IExtensionValue value)
        // //   where TTarget : IExtendableMessage<TTarget>
        // // {
        // //   if (set != null)
        // //     return set.ValuesByNumber.TryGetValue(extension.FieldNumber, out value);
        // //   value = (IExtensionValue) null;
        // //   return false;
        // // }
        // //
        // // /// <summary>Gets the value of the specified extension</summary>
        // // public static TValue Get<TTarget, TValue>(
        // //   ref ExtensionSet<TTarget> set,
        // //   Extension<TTarget, TValue> extension)
        // //   where TTarget : IExtendableMessage<TTarget>
        // // {
        // //   IExtensionValue extensionValue1;
        // //   if (!ExtensionSet.TryGetValue<TTarget>(ref set, (Extension) extension, out extensionValue1))
        // //     return extension.DefaultValue;
        // //   if (extensionValue1 is ExtensionValue<TValue> extensionValue2)
        // //     return extensionValue2.GetValue();
        // //   if (extensionValue1.GetValue() is TValue obj)
        // //     return obj;
        // //   TypeInfo typeInfo = extensionValue1.GetType().GetTypeInfo();
        // //   if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof (ExtensionValue<>))
        // //     throw new InvalidOperationException("The stored extension value has a type of '" + typeInfo.GenericTypeArguments[0].AssemblyQualifiedName + "'. This a different from the requested type of '" + typeof (TValue).AssemblyQualifiedName + "'.");
        // //   throw new InvalidOperationException("Unexpected extension value type: " + typeInfo.AssemblyQualifiedName);
        // // }
        // //
        // // /// <summary>
        // // /// Gets the value of the specified repeated extension or null if it doesn't exist in this set
        // // /// </summary>
        // // public static RepeatedField<TValue> Get<TTarget, TValue>(
        // //   ref ExtensionSet<TTarget> set,
        // //   RepeatedExtension<TTarget, TValue> extension)
        // //   where TTarget : IExtendableMessage<TTarget>
        // // {
        // //   IExtensionValue extensionValue;
        // //   if (!ExtensionSet.TryGetValue<TTarget>(ref set, (Extension) extension, out extensionValue))
        // //     return (RepeatedField<TValue>) null;
        // //   if (extensionValue is RepeatedExtensionValue<TValue> repeatedExtensionValue)
        // //     return repeatedExtensionValue.GetValue();
        // //   TypeInfo typeInfo = extensionValue.GetType().GetTypeInfo();
        // //   if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof (RepeatedExtensionValue<>))
        // //     throw new InvalidOperationException("The stored extension value has a type of '" + typeInfo.GenericTypeArguments[0].AssemblyQualifiedName + "'. This a different from the requested type of '" + typeof (TValue).AssemblyQualifiedName + "'.");
        // //   throw new InvalidOperationException("Unexpected extension value type: " + typeInfo.AssemblyQualifiedName);
        // // }
        //
        //
        // // DescriptorProtos.FieldOptions options = constraintFieldDesc.getOptions();
        // // // If the protovalidate field option is unknown, reparse options using our
        // // // extension registry.
        // // if (options.getUnknownFields().hasField(ValidateProto.predefined.getNumber())) {
        // //     try {
        // //         options = DescriptorProtos.FieldOptions.parseFrom(options.toByteString(), EXTENSION_REGISTRY);
        // //     } catch (InvalidProtocolBufferException e) {
        // //         throw new CompilationException("Failed to parse field options", e);
        // //     }
        // // }
        // // if (!options.hasExtension(ValidateProto.predefined)) {
        // //     return null;
        // // }
        //
        //
        // //
        // //
        // //
        // // var oneofFieldDescriptor = fieldOneofs[0].Accessor.GetCaseFieldDescriptor(fieldConstraints);
        // // if (oneofFieldDescriptor == null)
        // // {
        // //     return null;
        // // }
        // //
        // // // Get the expected constraint descriptor based on the provided field descriptor and the flag
        // // // indicating whether it is for items.
        // // var expectedConstraintDescriptor = DescriptorMappings.GetExpectedConstraintDescriptor(fieldDescriptor, forItems);
        // // if (expectedConstraintDescriptor == null)
        // // {
        // //     return null;
        // // }
        // //
        // // if (oneofFieldDescriptor.FullName != expectedConstraintDescriptor.FullName)
        // // {
        // //     // If the expected constraint does not match the actual oneof constraint, throw a
        // //     // CompilationError.
        // //     throw new CompilationException($"Expected constraint '{expectedConstraintDescriptor.FullName}', got '{oneofFieldDescriptor.FullName}' on field '{fieldDescriptor.FullName}'.");
        // // }
        // //
        // // var typedFieldConstraints = (IMessage)oneofFieldDescriptor.Accessor.GetValue(fieldConstraints);
        //
        //
        //
        // // var validatorOptions = new ValidatorOptions
        // // {
        // //     FileDescriptors = new[] { PredefinedReflection.Descriptor },
        // //     PreLoadDescriptors = true,
        // //     DisableLazy = false,
        // //     Extensions = new Extension[] { Buf.Validate.PredefinedExtensions.Uint64EvenProto2, Buf.Validate.PredefinedExtensions.DoubleAbsRangeProto2 }
        // // };
        // //
        // //
        // //
        // //
        // // var validator = new Validator(validatorOptions);
        // //
        // // var message = new TestUInt64EvenValidatorMessage();
        // // message.Val = input;
        // //
        // //
        // // var validationResult = validator.Validate(message, false);
        // //
        // //
        // // Assert.That(validationResult.IsSuccess, Is.EqualTo(shouldPass));
    }

    [Test]
    public void TestExtensionRegistry1()
    {
        var extensions = Buf.Validate.Conformance.Cases.IgnoreProtoEditionsReflection.Descriptor.Extensions.UnorderedExtensions ?? new List<FieldDescriptor>();

        //EditionsRepeatedItemIgnoreEmpty
        var messageDescriptor = EditionsMapKeyIgnoreEmpty.Descriptor;
        var fieldDescriptor = messageDescriptor.FindFieldByNumber(1);
        var fieldOptions = fieldDescriptor.GetOptions();
        var fieldConstraints = fieldOptions.GetExtension(ValidateExtensions.Field);
        var fieldConstraintDescriptor = ((IMessage)fieldConstraints).Descriptor;
        var fieldOneOfsDescriptor = fieldConstraintDescriptor.Oneofs[0];
        var oneofFieldDescriptor = fieldOneOfsDescriptor.Accessor.GetCaseFieldDescriptor(fieldConstraints);
        var typedFieldConstraints = (IMessage)oneofFieldDescriptor.Accessor.GetValue(fieldConstraints);


        var predefinedConstraints = GetPredefinedConstraints(extensions, typedFieldConstraints);

        //
        // foreach (var extensionFieldDescriptor in extensions)
        // {
        //     var targetType = extensionFieldDescriptor.ExtendeeType.ClrType;
        //     if (constraintType != targetType)
        //     {
        //         continue;
        //     }
        //
        //     var extensionOptions = extensionFieldDescriptor.GetOptions();
        //     var predefinedConstraints = extensionOptions.GetExtension(ValidateExtensions.Predefined);
        //     if (predefinedConstraints == null)
        //     {
        //         continue;
        //     }
        //
        //     var extension = extensionFieldDescriptor.Extension;
        //     var extensionType = extension.GetType();
        //     var extensionTypeGenericArguments = extensionType.GetGenericArguments();
        //     var extensionValueType = extensionTypeGenericArguments[1];
        //     
        //     
        //     //this is to get the value for the extension to pass into the CEL
        //     var hasExtensionMethod = extendableMessageType.GetMethod("HasExtension", BindingFlags.Instance | BindingFlags.Public);
        //     if (hasExtensionMethod == null)
        //     {
        //         throw new Exception("HasExtension method not found on type.");
        //     }
        //
        //     var genericHasExtensionMethod = hasExtensionMethod.MakeGenericMethod(new System.Type[] { extensionValueType });
        //
        //     var hasExtension = (bool?)genericHasExtensionMethod.Invoke(typedFieldConstraints, new object?[] { extension }) ?? false;
        //     if (!hasExtension)
        //     {
        //         continue;
        //     }
        //
        //     var getExtensionValueMethod = extendableMessageType.GetMethods().FirstOrDefault(c => c.Name == "GetExtension" && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extensionType.GetGenericTypeDefinition());
        //     if (getExtensionValueMethod == null)
        //     {
        //         throw new Exception("GetExtension method not found on type.");
        //     }
        //
        //     var genericGetExtensionValueMethod = getExtensionValueMethod.MakeGenericMethod(extensionValueType);
        //
        //         
        //     var extensionValue = genericGetExtensionValueMethod.Invoke(typedFieldConstraints, new object?[] { extension });
        //
        // }

    }

    private List<Tuple<PredefinedConstraints, object?>> GetPredefinedConstraints(IList<FieldDescriptor> extensions, IMessage typedFieldConstraints)
    {
        var results = new List<Tuple<PredefinedConstraints, object?>>();

        var constraintType = typedFieldConstraints.GetType();

        var matchingExtensions = extensions.Where(c => c.IsExtension && c.ExtendeeType.ClrType == constraintType).ToList();

        foreach (var extensionFieldDescriptor in matchingExtensions)
        {
            var targetType = extensionFieldDescriptor.ExtendeeType.ClrType;
            if (constraintType != targetType)
            {
                continue;
            }

            var extensionOptions = extensionFieldDescriptor.GetOptions();
            var predefinedConstraints = extensionOptions.GetExtension(ValidateExtensions.Predefined);
            if (predefinedConstraints == null)
            {
                continue;
            }

            var extension = extensionFieldDescriptor.Extension;
            var extensionType = extension.GetType();
            var extensionTypeGenericArguments = extensionType.GetGenericArguments();
            var extensionValueType = extensionTypeGenericArguments[1];

            //this is to get the value for the extension to pass into the CEL
            var extendableMessageType = typeof(IExtendableMessage<>).MakeGenericType(constraintType);
            var hasExtensionMethod = extendableMessageType.GetMethod("HasExtension", BindingFlags.Instance | BindingFlags.Public);
            if (hasExtensionMethod == null)
            {
                throw new Exception("HasExtension method not found on type.");
            }

            var genericHasExtensionMethod = hasExtensionMethod.MakeGenericMethod(new System.Type[] { extensionValueType });

            var hasExtension = (bool?)genericHasExtensionMethod.Invoke(typedFieldConstraints, new object?[] { extension }) ?? false;
            if (!hasExtension)
            {
                continue;
            }

            var getExtensionValueMethod = extendableMessageType.GetMethods().FirstOrDefault(c => c.Name == "GetExtension" && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extensionType.GetGenericTypeDefinition());
            if (getExtensionValueMethod == null)
            {
                throw new Exception("GetExtension method not found on type.");
            }

            var genericGetExtensionValueMethod = getExtensionValueMethod.MakeGenericMethod(extensionValueType);

            var extensionValue = genericGetExtensionValueMethod.Invoke(typedFieldConstraints, new object?[] { extension });
            results.Add(new Tuple<PredefinedConstraints, object?>(predefinedConstraints, extensionValue));
        }

        return results;
    }

    private System.Type GetExtensionTargetType(Extension extension)
    {
        var propertyInfo = typeof(Extension).GetProperty("TargetType", BindingFlags.Instance | BindingFlags.NonPublic);
        return (System.Type)propertyInfo!.GetValue(extension)!;
    }
}