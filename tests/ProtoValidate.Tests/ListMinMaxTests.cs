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

using System.Collections;
using buf.validate;
using NUnit.Framework;

namespace ProtoValidate.Tests;

[TestFixture]
public class ListMinMaxTests
{
    [Test]
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, false)]
    public void TestMinMaxListConstraints(int items, bool isValid)
    {
        //no items
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = new[] { TestReflection.Descriptor },
            PreLoadDescriptors = true,
            DisableLazy = false
        };

        var validator = new Validator(validatorOptions);


        var message = new ListMinMaxTest();
        for (var i = 0; i < items; i++)
        {
            message.Items.Add($"string-value-{i}");
        }

        var validationResultFailFast = validator.Validate(message, true);
        Assert.That(validationResultFailFast.IsSuccess, Is.EqualTo(isValid));

        
        var validationResult = validator.Validate(message, false);
        Assert.That(validationResult.IsSuccess, Is.EqualTo(isValid));
    }
}