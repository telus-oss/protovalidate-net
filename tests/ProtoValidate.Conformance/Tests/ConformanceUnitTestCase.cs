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

using Buf.Validate.Conformance.Harness;
using Google.Protobuf.WellKnownTypes;

namespace ProtoValidate.Conformance.Tests;

public class ConformanceUnitTestCase
{
    public ConformanceUnitTestCase(string suiteName, string caseName, TestResult expectedResult, Any input, CaseResult? caseResult)
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
    public CaseResult? CaseResult { get; }

    public override string ToString()
    {
        return $"{SuiteName}, {CaseName}";
    }
}