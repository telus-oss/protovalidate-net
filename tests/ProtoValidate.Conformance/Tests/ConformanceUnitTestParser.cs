using System.Reflection;
using System.Text;
using Buf.Validate.Conformance.Harness;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

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