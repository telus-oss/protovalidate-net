# See https://tech.davis-hansson.com/p/make/
SHELL := bash
.DELETE_ON_ERROR:
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := all
MAKEFLAGS += --warn-undefined-variables
MAKEFLAGS += --no-builtin-rules
MAKEFLAGS += --no-print-directory
COPYRIGHT_YEARS := 2023
LICENSE_IGNORE :=
BIN = tmp
GO ?= go
ARGS ?= --strict_message --timeout 10s
PROTOVALIDATE_VERSION ?= v0.5.4


.PHONY: conformance-windows-net48
conformance-windows-net48: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	$(BIN)\protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net48/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-windows-net60
conformance-windows-net60: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	$(BIN)\protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net6.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-windows-net70
conformance-windows-net70: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	$(BIN)\protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net7.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-windows-net80
conformance-windows-net80: $(BIN)/protovalidate-conformance-windows  ## Execute conformance tests.	
	$(BIN)\protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net8.0/win-x64/publish/ProtoValidate.Conformance.exe


.PHONY: conformance-mingw-net48
conformance-mingw-net48: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net48/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-mingw-net60
conformance-mingw-net60: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net6.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-mingw-net70
conformance-mingw-net70: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net7.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-mingw-net80
conformance-mingw-net80: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance.exe $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net8.0/win-x64/publish/ProtoValidate.Conformance.exe

.PHONY: conformance-linux-net60
conformance-linux-net60: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net6.0/linux-x64/publish/ProtoValidate.Conformance

.PHONY: conformance-linux-net70
conformance-linux-net70: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net7.0/linux-x64/publish/ProtoValidate.Conformance

.PHONY: conformance-linux-net80
conformance-linux-net80: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) ./tests/ProtoValidate.Conformance/bin/Release/net8.0/linux-x64/publish/ProtoValidate.Conformance


.PHONY: conformance-test-dump
conformance-test-dump: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) --dump --proto > ./tests/ProtoValidate.Conformance/Tests/Data/conformance.pb$(BIN)


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

$(BIN)/protovalidate-conformance-windows: Makefile
	SET "GOBIN=$(abspath $(BIN))" && $(GO) install github.com/bufbuild/protovalidate/tools/protovalidate-conformance@$(PROTOVALIDATE_VERSION)

