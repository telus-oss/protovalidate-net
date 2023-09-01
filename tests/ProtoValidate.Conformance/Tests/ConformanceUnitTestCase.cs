using Buf.Validate.Conformance.Harness;
using Google.Protobuf.WellKnownTypes;

namespace ProtoValidate.Conformance.Tests;

public class ConformanceUnitTestCase
{
    public ConformanceUnitTestCase(string suiteName, string caseName, TestResult expectedResult, Any input)
    {
        SuiteName = suiteName;
        CaseName = caseName;
        ExpectedResult = expectedResult;
        Input = input;
    }

    public string SuiteName { get; set; }
    public string CaseName { get; set; }
    public TestResult ExpectedResult { get; set; }
    public Any Input { get; set; }

    public override string ToString()
    {
        return $"{SuiteName}, {CaseName}";
    }

}