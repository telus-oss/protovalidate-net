using System.Reflection;
using Buf.Validate;
using Buf.Validate.Conformance.Harness;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using ProtoValidate.Conformance.Tests;
using ProtoValidate.Exceptions;

namespace ProtoValidate.Conformance;

internal class Program
{
    private static int Main(string[] args)
    {
        //ConformanceUnitTestParser.GetTestCases();

        // Thread.Sleep(15000);

        // using (var stdin = Console.OpenStandardInput())
        // {
        // using (var input = new System.IO.FileStream(@"D:\OpenSourceLibraries\protovalidate-net\.tmp\bin\input.bin", FileMode.Open))
        // {
        //     var request = TestConformanceRequest.Parser.ParseFrom(input);
        //
        // }
        // using (var input = new System.IO.FileStream(@"D:\OpenSourceLibraries\protovalidate-net\.tmp\bin\output.bin", FileMode.Open))
        // {
        //     var response = TestConformanceResponse.Parser.ParseFrom(input);
        //
        // }
        //


        using (var stdin = Console.OpenStandardInput())
        {
            using (var stdout = Console.OpenStandardOutput())
            {

               
                var request = TestConformanceRequest.Parser.ParseFrom(stdin);

                if (request == null)
                {
                    return 1;
                }

                var response = TestConformance(request);

                response.WriteTo(stdout);
                stdout.Flush();
                return 0;
            }
        }
    }

    private static TestConformanceResponse TestConformance(TestConformanceRequest request)
    {
        var extensionRegistry = new ExtensionRegistry
        {
            ValidateExtensions.Message,
            ValidateExtensions.Field,
            ValidateExtensions.Oneof
        };

        var remoteFileDescriptors = FileDescriptor.BuildFromByteStrings(request.Fdset.File.Select(c => c.ToByteString()), extensionRegistry);
        var localFileDescriptors = FileDescriptorUtil.GetFileDescriptors().ToArray();

        //register the local descriptors first since they contain the CLR initialization that isn't present
        //in the serialized file descriptors.
        var combinedFileDescriptors = localFileDescriptors.Union(remoteFileDescriptors).ToArray();

        //build a type registry so that we can unpack the ANY types.
        var typeRegistry = TypeRegistry.FromFiles(combinedFileDescriptors);

        var validatorOptions = new ValidatorOptions()
        {
            FileDescriptors = combinedFileDescriptors
        };

        var validator = new Validator(validatorOptions);
     
        var response = new TestConformanceResponse();
        foreach (var requestKvp in request.Cases)
        {
            var testResult = TestCase(validator, typeRegistry, requestKvp.Value);
            response.Results.Add(requestKvp.Key, testResult);
        }

        return response;
    }


    private static TestResult TestCase(Validator validator, TypeRegistry typeRegistry, Any testCase)
    {
        if (testCase == null)
        {
            throw new ArgumentNullException(nameof(testCase));
        }

        var message = testCase.Unpack(typeRegistry);
        return Validate(validator, message);
    }

    private static TestResult Validate(Validator validator, IMessage dynamicMessage)
    {
        try
        {
            var result = validator.Validate(dynamicMessage);
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
                CompilationError = e.Message
            };
        }
        catch (ExecutionException e)
        {
            return new TestResult
            {
                RuntimeError = e.Message
            };
        }
        catch (Exception e)
        {
            return UnexpectedErrorResult($"Unknown error: {e}");
        }
    }

    private static TestResult UnexpectedErrorResult(string message)
    {
        return new TestResult
        {
            UnexpectedError = message
        };
    }
}