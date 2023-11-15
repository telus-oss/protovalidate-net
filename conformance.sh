#!/usr/bin/env bash
set -euo pipefail
echo "running conformance"
dotnet tests/ProtoValidate.Conformance/bin/Debug/net8.0/ProtoValidate.Conformance.dll
