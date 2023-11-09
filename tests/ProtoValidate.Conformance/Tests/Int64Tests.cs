using Buf.Validate.Conformance.Cases;
using NUnit.Framework;
using ProtoValidate.Exceptions;

namespace ProtoValidate.Conformance.Tests;

[TestFixture]
public class Int64Tests
{
    [SetUp]
    public void SetupFixture()
    {
        Validator = new Validator();
    }

    private Validator? Validator { get; set; }

    [Test]
    public void Int64LTE()
    {
        var message = new Int64LTE
        {
            Val = 65
        };

        var validationResult = Validator!.Validate(message, false);
        Assert.IsFalse(validationResult.IsSuccess);
    }
    [Test]
    public void UInt64GTLT()
    {
        // message UInt64GTLT {
        //     uint64 val = 1 [(buf.validate.field).uint64 = {
        //         gt: 5,
        //         lt: 10
        //     }];
        // }


        var message = new UInt64GTLT
        {
            Val = 11
        };

        var validationResult = Validator!.Validate(message, false);
        Assert.IsFalse(validationResult.IsSuccess);
        Console.WriteLine(validationResult);
    }
    [Test]
    public void UInt64In_In()
    {
        // message UInt64In {
        //     uint64 val = 1 [(buf.validate.field).uint64 = {
        //         in: [
        //         2,
        //         3
        //             ]
        //     }];
        // }


        var message = new UInt64In
        {
            Val = 2
        };

        var validationResult = Validator!.Validate(message, false);
        Assert.IsTrue(validationResult.IsSuccess);
        Console.WriteLine(validationResult);
    }
    [Test]
    public void UInt64In_NotIn()
    {
        // message UInt64In {
        //     uint64 val = 1 [(buf.validate.field).uint64 = {
        //         in: [
        //         2,
        //         3
        //             ]
        //     }];
        // }


        var message = new UInt64In
        {
            Val = 4
        };

        var validationResult = Validator!.Validate(message, false);
        Assert.IsFalse(validationResult.IsSuccess);
        Console.WriteLine(validationResult);
    }

    [Test]
    public void Double_IncorrectType()
    {
        // message DoubleIncorrectType {
        //     double val = 1 [(buf.validate.field).float.gt = 0];
        // }



        var message = new DoubleIncorrectType
        {
            Val = 123
        };

        try
        {
            var validationResult = Validator!.Validate(message, false);
            Assert.Fail("Expected compilation exception.");
        }
        catch (CompilationException)
        {
            
        }
    }
}