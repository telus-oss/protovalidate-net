// Copyright 2023 TELUS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        catch (CompilationException) { }
    }

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
    }
}