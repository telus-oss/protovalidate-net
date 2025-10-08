// Copyright 2023-2025 TELUS
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
using Buf.Validate.Conformance.Cases;
using Buf.Validate.Conformance.Harness;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using ProtoValidate.Exceptions;

namespace ProtoValidate.Conformance;

internal class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            using (var fs = File.OpenRead(args[0]))
            {
                var request = TestConformanceRequest.Parser.ParseFrom(fs);
                if (request == null)
                {
                    return 1;
                }

                var response = TestConformance(request);

                var formatter = new JsonFormatter(new JsonFormatter.Settings(true).WithIndentation());
                var json = formatter.Format(response);
                Console.WriteLine(json);
                return 0;
            }
        }

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
            ValidateExtensions.Oneof,
            PredefinedRulesProto2Extensions.BoolFalseProto2,
            PredefinedRulesProto2Extensions.BytesValidPathProto2,
            PredefinedRulesProto2Extensions.DoubleAbsRangeProto2,
            PredefinedRulesProto2Extensions.DurationTooLongProto2,
            PredefinedRulesProto2Extensions.EnumNonZeroProto2,
            PredefinedRulesProto2Extensions.Fixed32EvenProto2,
            PredefinedRulesProto2Extensions.Fixed64EvenProto2,
            PredefinedRulesProto2Extensions.FloatAbsRangeProto2,
            PredefinedRulesProto2Extensions.Int32AbsInProto2,
            PredefinedRulesProto2Extensions.Int64AbsInProto2,
            PredefinedRulesProto2Extensions.RepeatedAtLeastFiveProto2,
            PredefinedRulesProto2Extensions.Sfixed32EvenProto2
        };

        var remoteFileDescriptors = FileDescriptor.BuildFromByteStrings(request.Fdset.File.Select(c => c.ToByteString()), extensionRegistry);
        var localFileDescriptors = FileDescriptorUtil.GetFileDescriptors().ToArray();

        //register the local descriptors first since they contain the CLR initialization that isn't present
        //in the serialized file descriptors.
        var combinedFileDescriptors = localFileDescriptors.Union(remoteFileDescriptors).ToArray();

        //build a type registry so that we can unpack the ANY types.
        var typeRegistry = TypeRegistry.FromFiles(combinedFileDescriptors);

        var validatorOptions = new ValidatorOptions
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
            var result = validator.Validate(dynamicMessage, false);
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