# See https://tech.davis-hansson.com/p/make/
SHELL := bash
.DELETE_ON_ERROR:
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := all
MAKEFLAGS += --warn-undefined-variables
MAKEFLAGS += --no-builtin-rules
MAKEFLAGS += --no-print-directory
COPYRIGHT_YEARS := 2023-2025
LICENSE_IGNORE :=
BIN = tmp
GO ?= go
ARGS ?= --timeout 10s
PROTOVALIDATE_VERSION ?= v1.0.0


.PHONY: conformance-windows-net48
conformance-windows-net48: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	$(BIN)\protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net48/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-windows-net80
conformance-windows-net80: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	$(BIN)\protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net8.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-mingw-net48
conformance-mingw-net48: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net48/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-mingw-net80
conformance-mingw-net80: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net8.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-linux-net80
conformance-linux-net80: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net8.0/linux-x64/publish/ProtoValidate.Conformance

.PHONY: conformance-test-dump
conformance-test-dump: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	-$(BIN)/protovalidate-conformance $(ARGS) --dump --proto > ./tests/ProtoValidate.Conformance/Tests/Data/conformance.pbbin

.PHONY: conformance-test-dump-windows
conformance-test-dump-windows: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	-$(BIN)/protovalidate-conformance.exe $(ARGS) --dump --proto > ./tests/ProtoValidate.Conformance/Tests/Data/conformance.pbbin


.PHONY: generate-license
generate-license: $(BIN)/license-header  ## Generates license headers for all source files.
	$(BIN)/license-header \
		--license-type apache \
		--copyright-holder "TELUS" \
		--year-range "$(COPYRIGHT_YEARS)" $(LICENSE_IGNORE)

.PHONY: help
help:  ## Describe useful make targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "%-15s %s\n", $$1, $$2}'

$(BIN):
	@mkdir -p $(BIN)

$(BIN)/buf: $(BIN) Makefile
	GOBIN=$(abspath $(@D)) $(GO) install \
		  github.com/bufbuild/buf/cmd/buf@latest

$(BIN)/license-header: $(BIN) Makefile
	GOBIN=$(abspath $(@D)) $(GO) install \
		  github.com/bufbuild/buf/private/pkg/licenseheader/cmd/license-header@latest

$(BIN)/protovalidate-conformance: $(BIN) Makefile
	GOBIN=$(abspath $(BIN)) $(GO) install \
		github.com/bufbuild/protovalidate/tools/protovalidate-conformance@$(PROTOVALIDATE_VERSION)

$(BIN)/protovalidate-conformance-windows: $(BIN) Makefile
	GOBIN=$(abspath $(BIN)) $(GO) install github.com/bufbuild/protovalidate/tools/protovalidate-conformance@$(PROTOVALIDATE_VERSION)

