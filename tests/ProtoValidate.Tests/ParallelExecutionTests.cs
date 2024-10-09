// Copyright 2024 TELUS
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
using Buf.Validate;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;

namespace ProtoValidate.Tests;

[TestFixture]
public class ParallelExecutionTests
{
    [Test]
    public void TestParallelValidation_Transaction()
    {
        //run this test a lot of times to ensure we have no race conditions
        for (var i = 0; i < 100; i++)
        {
            var validatorOptions = new ValidatorOptions();
            validatorOptions.PreLoadDescriptors = false;
            validatorOptions.FileDescriptors = new[] { TestReflection.Descriptor };

            var validator = new Validator();
            
            var t = new Transaction();
            t.PurchaseDate = Timestamp.FromDateTime(DateTime.UtcNow);
            t.DeliveryDate = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(1));
            

            //validate simultaneously to trigger a race condition in this validator.
            Parallel.For(0, 20, c => { validator.Validate(t, false); });
        }
    }

    [Test]
    public void TestParallelValidation_NestedType()
    {
        //run this test a lot of times to ensure we have no race conditions
        for (var i = 0; i < 100; i++)
        {
            var validatorOptions = new ValidatorOptions();
            validatorOptions.PreLoadDescriptors = false;
            validatorOptions.FileDescriptors = new[] { NestReflection.Descriptor };

            var validator = new Validator();
            
            var m = new NestedMessageLevel1();
            
            //validate simultaneously to trigger a race condition in this validator.
            Parallel.For(0, 20, c => { validator.Validate(m, false); });
        }
    }
}