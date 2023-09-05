using Google.Protobuf.Reflection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buf.Validate;
using Buf.Validate.Conformance.Harness;
using Google.Protobuf;
using ProtoValidate.Exceptions;

namespace ProtoValidate.Conformance.Tests
{
    [TestFixture]
    public class ConformanceUnitTests
    {
        private TypeRegistry? TypeRegistry { get; } = FileDescriptorUtil.GetTypeRegistry();
        private Validator? Validator { get; set; }

        [SetUp]
        public void Setup()
        {
            var config = new Config();
            Validator = new Validator(config);

        }

        [Test]
        [TestCaseSource(typeof(ConformanceUnitTestParser), nameof(ConformanceUnitTestParser.GetTestCases), Category = "Conformance Unit Tests")]
        public void SimpleTest(ConformanceUnitTestCase testCase)
        {
            var testData = testCase!.Input?.Unpack(TypeRegistry)!;

            var testResults = Validate(testData!);

            if (!string.IsNullOrWhiteSpace(testResults.RuntimeError))
            {
                Assert.Fail(testResults.RuntimeError);
                return;
            }
            if (!string.IsNullOrWhiteSpace(testResults.CompilationError))
            {
                Assert.Fail(testResults.CompilationError);
                return;
            }
            if (!string.IsNullOrWhiteSpace(testResults.UnexpectedError))
            {
                Assert.Fail(testResults.UnexpectedError);
                return;
            }

            if (!testCase.ExpectedResult.Success || !testResults.Success)
            {
                var settings = JsonFormatter.Settings.Default.WithIndentation().WithTypeRegistry(TypeRegistry);
                JsonFormatter formatter = new JsonFormatter(settings);
                var inputJson = "";

                try
                {
                    inputJson = formatter.Format(testData);
                }
                catch (InvalidOperationException) { }

                Console.WriteLine("Input");
                Console.WriteLine(testData.GetType().Name);
                Console.WriteLine(inputJson);
                Console.WriteLine();



                Console.WriteLine("Expected");
                if (testCase.ExpectedResult.ValidationError != null)
                {
                    foreach (var violation in testCase.ExpectedResult.ValidationError.Violations_)
                    {
                        Console.WriteLine("{0} {1} {2}", violation.ConstraintId, violation.ForKey, violation.FieldPath);
                        Console.WriteLine(violation.Message);
                        Console.WriteLine();
                    }
                }

                Console.WriteLine("Actual");
                if (testResults.ValidationError != null)
                {
                    foreach (var violation in testResults.ValidationError.Violations_)
                    {
                        Console.WriteLine("{0} {1} {2}", violation.ConstraintId, violation, violation.FieldPath);
                        Console.WriteLine(violation.Message);
                        Console.WriteLine();
                    }
                }
            }


            if (testCase!.ExpectedResult!.Success)
            {
                Assert.IsTrue(testResults.Success);
            }
            else
            {
                Assert.IsFalse(testResults.Success);
                Assert.AreEqual(testCase.ExpectedResult?.ValidationError?.Violations_.Count ?? 0, testResults?.ValidationError?.Violations_.Count ?? 0);
            }
        }

        private TestResult Validate(IMessage dynamicMessage)
        {
            try
            {
                var result = Validator!.Validate(dynamicMessage);
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
                    RuntimeError = e.ToString()
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
    }
}
