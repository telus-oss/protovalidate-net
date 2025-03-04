using Buf.Validate.CelRepeatedFieldTransformAndUnique;
using NUnit.Framework;

namespace ProtoValidate.Tests;

[TestFixture]
public class CelRepeatedFieldTransformAndUniqueTest
{
    [Test]
    public void Validate_Unique_Repeated_Strings_Success()
    {
        //this is to test infinite recursion bug in validator loading mappings.
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { CelRepeatedFieldTransformAndUniqueReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);

        var message = new Book();
        message.Genres.Add("genre1");
        message.Genres.Add("genre2");

        var validationResult = validator.Validate(message, false);
        Assert.That(validationResult.IsSuccess, Is.True);
    }

    [Test]
    public void Validate_Unique_Repeated_Strings_Failure()
    {
        //this is to test infinite recursion bug in validator loading mappings.
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { CelRepeatedFieldTransformAndUniqueReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);

        var message = new Book();
        message.Genres.Add("genre");
        message.Genres.Add("genre");

        var validationResult = validator.Validate(message, false);
        Assert.That(validationResult.IsSuccess, Is.False);
    }

    [Test]
    public void Validate_Unique_Map_Success()
    {
        //this is to test infinite recursion bug in validator loading mappings.
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { CelRepeatedFieldTransformAndUniqueReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);

        var message = new Book();
        message.Authors.Add(new Author { Name = "Author1" });
        message.Authors.Add(new Author { Name = "Author2" });

        var validationResult = validator.Validate(message, false);
        Assert.That(validationResult.IsSuccess, Is.True);
    }

    [Test]
    public void Validate_Unique_Map_Failure()
    {
        //this is to test infinite recursion bug in validator loading mappings.
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { CelRepeatedFieldTransformAndUniqueReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);

        var message = new Book();
        message.Authors.Add(new Author { Name = "Author" });
        message.Authors.Add(new Author { Name = "Author" });

        var validationResult = validator.Validate(message, false);
        Assert.That(validationResult.IsSuccess, Is.False);
    }
}