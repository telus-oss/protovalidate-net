# See https://tech.davis-hansson.com/p/make/
SHELL := bash
.DELETE_ON_ERROR:
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := all
MAKEFLAGS += --warn-undefined-variables
MAKEFLAGS += --no-builtin-rules
MAKEFLAGS += --no-print-directory
BIN := .tmp/bin
COPYRIGHT_YEARS := 2023
LICENSE_IGNORE :=
GO ?= go
ARGS ?= --strict_message
PROTOVALIDATE_VERSION ?= v0.5.4

.PHONY: all
all: lint generate build docs conformance  ## Run all tests and lint (default)

.PHONY: checkgenerate
checkgenerate: generate  ## Checks if `make generate` produces a diff.
	@# Used in CI to verify that `make generate` doesn't produce a diff.
	test -z "$$(git status --porcelain | tee /dev/stderr)"

.PHONY: clean
clean:  ## Delete intermediate build artifacts
	@# -X only removes untracked files, -d recurses into directories, -f actually removes files/dirs
	git clean -Xdf

.PHONY: conformance
conformance: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) ./tests/ProtoValidate.Conformance/bin/Debug/net8.0/win-x64/ProtoValidate.Conformance.exe

.PHONY: conformance-test-dump
conformance-test-dump: $(BIN)/protovalidate-conformance  ## Execute conformance tests.	
	$(BIN)/protovalidate-conformance $(ARGS) --dump --proto > ./tests/ProtoValidate.Conformance/Tests/Data/conformance.pbbin


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

