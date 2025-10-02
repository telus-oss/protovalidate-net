using Buf.Validate;
using Buf.Validate.Conformance.Cases;
using NUnit.Framework;

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
            FileDescriptors = new[] { PredefinedReflection.Descriptor, IgnoreProtoEditionsReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);

        var message = new TestUInt64EvenValidatorMessage();
        message.Val = input;

        var validationResult = validator.Validate(message, false);

        Assert.That(validationResult.IsSuccess, Is.EqualTo(shouldPass));
    }
}