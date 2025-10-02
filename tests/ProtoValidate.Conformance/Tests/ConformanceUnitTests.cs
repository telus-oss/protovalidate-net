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

using Buf.Validate;
using Buf.Validate.Conformance.Harness;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using NUnit.Framework;
using ProtoValidate.Exceptions;

namespace ProtoValidate.Conformance.Tests;

[TestFixture]
public class ConformanceUnitTests
{
    [SetUp]
    public void Setup()
    {
        var validatorOptions = new ValidatorOptions
        {
            FileDescriptors = FileDescriptorUtil.GetFileDescriptors().ToList()
        };

        Validator = new Validator(validatorOptions);
    }

    private TypeRegistry? TypeRegistry { get; } = FileDescriptorUtil.GetTypeRegistry();
    private Validator? Validator { get; set; }

    private TestResult Validate(IMessage dynamicMessage, bool failFast)
    {
        try
        {
            var result = Validator!.Validate(dynamicMessage, failFast);
            var violations = result.Violations;
            if (violations.Count == 0)
            {
                return new TestResult
                {
                    Success = true
                };
            }

            var error = new Violations
            {
                Violations_ = { violations }
            };

            return new TestResult
            {
                ValidationError = error
            };
        }
        catch (CompilationException e)
        {
            return new TestResult
            {
                CompilationError = e.ToString()
            };
        }
        catch (ExecutionException e)
        {
            return new TestResult
            {
                RuntimeError = !string.IsNullOrWhiteSpace(e.SourceExpression?.Message) ? e.SourceExpression?.Message : e.Message
            };
        }
        catch (Exception e)
        {
            return new TestResult
            {
                UnexpectedError = e.ToString()
            };
        }
    }

    [Test]
    [TestCaseSource(typeof(ConformanceUnitTestParser), nameof(ConformanceUnitTestParser.GetTestCases), Category = "Conformance Unit Tests")]
    public void SimpleTest(ConformanceUnitTestCase testCase)
    {
        var testData = testCase!.Input?.Unpack(TypeRegistry)!;

        var testResults = Validate(testData!, false);

        //test for unexpected errors
        Assert.That(string.IsNullOrWhiteSpace(testCase.ExpectedResult?.UnexpectedError), Is.EqualTo(string.IsNullOrWhiteSpace(testResults.UnexpectedError)), testResults.UnexpectedError);

        //test for compilation errors
        Assert.That(string.IsNullOrWhiteSpace(testCase.ExpectedResult?.CompilationError), Is.EqualTo(string.IsNullOrWhiteSpace(testResults.CompilationError)), testResults.CompilationError);

        //test for Runtime.
        Assert.That(string.IsNullOrWhiteSpace(testCase.ExpectedResult?.RuntimeError), Is.EqualTo(string.IsNullOrWhiteSpace(testResults.RuntimeError)), testResults.RuntimeError);

        if (testCase.ExpectedResult == null)
        {
            Assert.Fail("We need expected result to have a value.");
            return;
        }

#if DEBUG
        if (!testCase.ExpectedResult.Success || !testResults.Success)
        {
            if (testCase.CaseResult != null)
            {
                var testCaseJson = JsonFormatter.Default.Format(testCase.CaseResult);
                Console.WriteLine(testCaseJson);
            }

            if (testCase.ExpectedResult.ValidationError != null
                && testResults.ValidationError != null
                && testCase.ExpectedResult.ValidationError.Violations_.Count != testResults.ValidationError.Violations_.Count)
            {
                foreach (var violation in testCase.ExpectedResult.ValidationError.Violations_)
                {
                    if (!testResults.ValidationError.Violations_.Any(c => c.RuleId == violation.RuleId))
                    {
                        Console.WriteLine($"Expected violation {violation.RuleId} but was not validated.");
                        Console.WriteLine();
                    }
                }

                foreach (var violation in testResults.ValidationError.Violations_)
                {
                    if (!testCase.ExpectedResult.ValidationError.Violations_.Any(c => c.RuleId == violation.RuleId))
                    {
                        Console.WriteLine($"Got violation {violation.RuleId} but was not expected.");
                        Console.WriteLine();
                    }
                }
            }


            var settings = JsonFormatter.Settings.Default.WithIndentation().WithTypeRegistry(TypeRegistry);
            var formatter = new JsonFormatter(settings);
            var inputJson = "";

            try
            {
                inputJson = formatter.Format(testData);
            }
            catch (InvalidOperationException)
            {
            }

            Console.WriteLine("Input");
            Console.WriteLine(testData.GetType().Name);
            Console.WriteLine(inputJson);
            Console.WriteLine();


            Console.WriteLine("Expected");
            if (testCase.ExpectedResult.ValidationError != null)
            {
                //fix the sorting
                foreach (var violation in testCase.ExpectedResult.ValidationError.Violations_.OrderBy(c => c.RuleId))
                {
                    Console.WriteLine("{0} {1}", violation.RuleId, violation.ForKey);
                    Console.WriteLine(violation.Message);
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Actual");
            if (testResults.ValidationError != null)
            {
                //fix the sorting
                foreach (var violation in testResults.ValidationError.Violations_.OrderBy(c => c.RuleId))
                {
                    Console.WriteLine(violation);
                    Console.WriteLine(violation.Value);
                }
            }
        }
#endif

        if (testCase!.ExpectedResult!.Success)
        {
            Assert.That(testResults.Success, Is.True);
        }
        else
        {
            Assert.That(testResults.Success, Is.False);
            Assert.That(testCase.ExpectedResult?.ValidationError?.Violations_.Count ?? 0, Is.EqualTo(testResults?.ValidationError?.Violations_.Count ?? 0));
        }
    }

    [Test]
    [TestCaseSource(typeof(ConformanceUnitTestParser), nameof(ConformanceUnitTestParser.GetTestCases), Category = "Conformance Unit Tests")]
    public void SimpleTestFailFast(ConformanceUnitTestCase testCase)
    {
        var testData = testCase!.Input?.Unpack(TypeRegistry)!;

        var testResults = Validate(testData!, true);

        //test for Runtime.
        Assert.That(string.IsNullOrWhiteSpace(testResults.RuntimeError), Is.EqualTo(string.IsNullOrWhiteSpace(testCase.ExpectedResult?.RuntimeError)));

        //test for compilation errors
        Assert.That(string.IsNullOrWhiteSpace(testResults.CompilationError), Is.EqualTo(string.IsNullOrWhiteSpace(testCase.ExpectedResult?.CompilationError)));

        //test for unexpected errors
        Assert.That(string.IsNullOrWhiteSpace(testResults.UnexpectedError), Is.EqualTo(string.IsNullOrWhiteSpace(testCase.ExpectedResult?.UnexpectedError)));

        if (testCase.ExpectedResult == null)
        {
            Assert.Fail("We need expected result to have a value.");
            return;
        }

        if (testCase!.ExpectedResult!.Success)
        {
            Assert.That(testResults.Success, Is.True);
            Assert.That(testResults.ValidationError?.Violations_, Is.Null);
        }
        else if (testCase.ExpectedResult.HasUnexpectedError)
        {
            Assert.That(testResults.Success, Is.False);
            Assert.That(testResults.ValidationError?.Violations_, Is.Null);
            Assert.That(testResults.HasUnexpectedError, Is.True);
        }
        else if (testCase.ExpectedResult.HasRuntimeError)
        {
            Assert.That(testResults.Success, Is.False);
            Assert.That(testResults.ValidationError?.Violations_, Is.Null);
            Assert.That(testResults.HasRuntimeError, Is.True);
        }
        else if (testCase.ExpectedResult.HasCompilationError)
        {
            Assert.That(testResults.Success, Is.False);
            Assert.That(testResults.ValidationError?.Violations_, Is.Null);
            Assert.That(testResults.HasCompilationError, Is.True);
        }
        else
        {
            Assert.That(testResults.Success, Is.False);
            Assert.That(testResults.ValidationError?.Violations_, Is.Not.Null);
            Assert.That(testResults.ValidationError!.Violations_.Count, Is.EqualTo(1));
        }
    }
}