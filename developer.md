# protovalidate-net developer quick reference


## To run conformance tests
* run `make conformance-windows-net48`
* run `make conformance-windows-net80`
* run `make conformance-mingw-net48`
* run `make conformance-mingw-net80`
* run `make conformance-linux-net80`

You can use cmd.exe to run make on Windows.

## To change .Net Versions
* Edit github actions for the environments
* Edit Makefile to configure conformance tests
* Edit .Net csproj files to reflect new target frameworks

## To change protovalidate version
* Edit Makefile version number
* run `make conformance-test-dump` on linux or `make conformance-test-dump-windows` on Windows to dump the conformance.pbbin file.
* Copy `tests/Protovalidate.Conformance/proto/buf/validate/conformance` folder from the official ProtoValidate source.  It will be needed for the new conformance tests.
