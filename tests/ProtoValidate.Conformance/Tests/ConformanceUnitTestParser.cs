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

using System.Reflection;
using Buf.Validate.Conformance.Harness;

namespace ProtoValidate.Conformance.Tests;

public class ConformanceUnitTestParser
{
    public static ConformanceUnitTestCase[] GetTestCases()
    {
        var testCases = new List<ConformanceUnitTestCase>();

        var resourceNames = GetResourceNames();
        foreach (var resourceName in resourceNames)
        {
            var resultSet = ParseTestProtoFile(resourceName);
            if (resultSet == null)
            {
                continue;
            }

            for (var i = 0; i < resultSet.Suites.Count; i++)
            {
                for (var j = 0; j < resultSet.Suites[i].Cases.Count; j++)
                {
                    var suite = resultSet.Suites[i];
                    var test = suite.Cases[j];


                    var suiteName = !string.IsNullOrWhiteSpace(suite.Name) ? suite.Name : "Suite " + (i + 1);
                    var testName = !string.IsNullOrWhiteSpace(test.Name) ? test.Name : "Test " + (j + 1);


                    var testCase = new ConformanceUnitTestCase(suiteName, testName, test.Wanted, test.Input);

                    testCases.Add(testCase);
                }
            }
        }

        return testCases.ToArray();
    }

    public static string[] GetResourceNames()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        var conformanceTestResourceNames = resourceNames.Where(c => c.StartsWith("ProtoValidate.Conformance.Tests.Data.", StringComparison.Ordinal)
                                                                    && c.EndsWith(".pbbin", StringComparison.Ordinal));

        return conformanceTestResourceNames.ToArray();
    }

    public static ResultSet? ParseTestProtoFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                return null;
            }

            return ResultSet.Parser.ParseFrom(stream);

            // var typeRegistry = FileDescriptorUtil.GetTypeRegistry();
            // var parser = new JsonParser(new JsonParser.Settings(20, typeRegistry));
            // StreamReader reader = new StreamReader(stream);
            // string payload = reader.ReadToEnd();
            //
            // var message = parser.Parse<ResultSet>(payload);
            //
            // return message;
        }
    }
}