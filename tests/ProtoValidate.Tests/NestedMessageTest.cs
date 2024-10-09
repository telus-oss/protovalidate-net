using Buf.Validate;
using NUnit.Framework;

namespace ProtoValidate.Tests;

[TestFixture]
public class NestedMessageTest
{
    [Test]
    public void Initialization_Of_Validator_Should_Work_With_Nested_Messages()
    {
        //this is to test infinite recursion bug in validator loading mappings.
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { NestReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);

        var nestedMessage = CreateNestedMessage();
        var validationResult =validator.Validate(nestedMessage, false);

    }

    [Test]
    public void Lazy_Initialization_Of_Validator_Should_Work_With_Nested_Messages()
    {
        //this is to test infinite recursion bug in validator loading mappings.
        var validator = new Validator();

        var nestedMessage = CreateNestedMessage();
        validator.Validate(nestedMessage, false);
    }

    [Test]
    public void Initialization_Of_Validator_Should_Work_With_Nested_Messages_With_Options_Disable_Lazy_True()
    {
        //this is to test infinite recursion bug in validator loading mappings.

        var validatorOptions = new ValidatorOptions
        {
            DisableLazy = true
        };
        var validator = new Validator(validatorOptions);

        var nestedMessage = CreateNestedMessage();
        validator.Validate(nestedMessage, false);
    }

    [Test]
    public void Initialization_Of_Validator_Should_Work_With_Nested_Messages_With_Options_Disable_Lazy_False()
    {
        //this is to test infinite recursion bug in validator loading mappings.

        var validatorOptions = new ValidatorOptions
        {
            DisableLazy = false
        };
        var validator = new Validator(validatorOptions);

        var nestedMessage = CreateNestedMessage();
        validator.Validate(nestedMessage, false);
    }


    private static NestedMessageLevel1 CreateNestedMessage()
    {
        var nestedMessage = new NestedMessageLevel1
        {
            Value = "a",
            Nest = new NestedMessageLevel2
            {
                Value = "b",
                Nest3List =
                {
                    new NestedMessageLevel3
                    {
                        Value = "c"
                    },
                    new NestedMessageLevel3
                    {
                        Value = "d",
                        Nest2 = new NestedMessageLevel2
                        {
                            Value = "e",
                            Nest3List =
                            {
                                new NestedMessageLevel3
                                {
                                    Value = "f"
                                },
                                new NestedMessageLevel3
                                {
                                    Value = "g"
                                }
                            }
                        }
                    }
                }
            }
        };
        return nestedMessage;
    }
}