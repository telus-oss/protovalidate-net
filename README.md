# protovalidate-net

`protovalidate-net` is the C# implementation of [`protovalidate`](https://github.com/bufbuild/protovalidate), designed to validate Protobuf messages at runtime based on user-defined validation constraints. Powered by Google's Common Expression Language ([CEL](https://github.com/google/cel-spec)), it provides a flexible and efficient foundation for defining and evaluating custom validation rules. The primary goal of `protovalidate` is to help developers ensure data consistency and integrity across the network without requiring generated code.

## The `protovalidate` project

Head over to the core [`protovalidate`](https://github.com/bufbuild/protovalidate/) repository for:

- [The API definition](https://github.com/bufbuild/protovalidate/tree/main/proto/protovalidate/buf/validate/validate.proto):
  used to describe validation constraints
- [Documentation](https://github.com/bufbuild/protovalidate/tree/main/docs): how to apply `protovalidate` effectively
- [Migration tooling](https://github.com/bufbuild/protovalidate/tree/main/docs/migrate.md): incrementally migrate
  from `protoc-gen-validate`
- [Conformance testing utilities](https://github.com/bufbuild/protovalidate/tree/main/docs/conformance.md): for
  acceptance testing of `protovalidate` implementations

Other `protovalidate` runtime implementations include:

- Go: [`protovalidate-go`](https://github.com/bufbuild/protovalidate-go)
- C++: [`protovalidate-cc`](https://github.com/bufbuild/protovalidate-cc)
- Java: [`protovalidate-java`](https://github.com/bufbuild/protovalidate-java)

## Installation

To install the package, use nuget:

```shell
nuget install ProtoValidate
```
## Usage

This example shows how to use ProtoValidate-net with dependency injection  Register ProtoValidate in your service collection and configure the options.
```csharp
using ProtoValidate;

// register ProtoValidate with no options
serviceCollection.AddProtoValidate();

// OR register ProtoValidate with options
serviceCollection.AddProtoValidate(options => {       
    // This setting is used to configure if it loads your validation descriptors upon creation of the validator.
    // True will load on creation
    // False will defer loading the validator until first run of the validation logic for that type.
    options.PreLoadDescriptors = true;

    // This setting will cause a compilation exception to be thrown if the message type you are validating hasn't been pre-loaded using the file descriptor list.
    options.DisableLazy = true;

    //register your file descriptors generated by Google.Protobuf library for your compiled .proto files
    options.FileDescriptors = new List<FileDescriptor>() {
        //your list of Protobuf File Descriptors here
    }
});
```

Example on how to validate a message using the IValidator instance from the service provider

```csharp
// define your Protobuf message that needs validation
var myMessageToValidate = new MyMessageThatNeedsValidation() {...};

// resolve IValidator from your service provider
// you can resolve it directly like this, or from the constructor using dependency injection
var validator = serviceProvider.GetRequiredService<ProtoValidate.IValidator>();

// flag to indicate if the validator should return on the first error (true) or validate all the fields and return all the errors in the message (false).
var failFast = true;

//validate the message
var violations = validator.Validate(myMessageToValidate, failFast);

//the violations contains the validation errors.
var hasViolations = violations.Violations.Count > 0;
```

Example on how to create a validator and validate a message without using dependency injection.

```csharp

var validatorOptions = new ProtoValidate.ValidatorOptions() {
    // This setting is used to configure if it loads your validation descriptors upon creation of the validator.
    // True will load on creation
    // False will defer loading the validator until first run of the validation logic for that type.
    PreLoadDescriptors = true,

    // This setting will cause a compilation exception to be thrown if the message type you are validating hasn't been pre-loaded using the file descriptor list.
    DisableLazy = true,

    //register your file descriptors generated by Google.Protobuf library for your compiled .proto files
    FileDescriptors = new List<FileDescriptor>() {
        //your list of Protobuf File Descriptors here
    }
};

//Instantiate the validator.  You should cache the validator for reuse.
var validator = new ProtoValidate.Validator(validatorOptions);

// flag to indicate if the validator should return on the first error (true) or validate all the fields and return all the errors in the message (false).
var failFast = true;

// define your Protobuf message that needs validation
var myMessageToValidate = new MyMessageThatNeedsValidation() {...};

//validate the message
var violations = validator.Validate(myMessageToValidate, failFast);

//the violations contains the validation errors.
var hasViolations = violations.Violations.Count > 0;

```





### Implementing validation constraints

Validation constraints are defined directly within `.proto` files. Documentation for adding constraints can be found in the `protovalidate` project [README](https://github.com/bufbuild/protovalidate) and its [comprehensive docs](https://github.com/bufbuild/protovalidate/tree/main/docs).

```protobuf
syntax = "proto3";

package my.package;

import "google/protobuf/timestamp.proto";
import "buf/validate/validate.proto";

message Transaction {
  uint64 id = 1 [(buf.validate.field).uint64.gt = 999];
  google.protobuf.Timestamp purchase_date = 2;
  google.protobuf.Timestamp delivery_date = 3;

  string price = 4 [(buf.validate.field).cel = {
    id: "transaction.price",
    message: "price must be positive and include a valid currency symbol ($ or £)",
    expression: "(this.startswith('$') or this.startswith('£')) and float(this[1:]) > 0"
  }];

  option (buf.validate.message).cel = {
    id: "transaction.delivery_date",
    message: "delivery date must be after purchase date",
    expression: "this.delivery_date > this.purchase_date"
  };
}
```

### Ecosystem

- [`protovalidate`](https://github.com/bufbuild/protovalidate) core repository
- [Buf](https://buf.build)
- [CEL Spec](https://github.com/google/cel-spec)
- [CEL C# implementation](https://github.com/telus-oss/cel-net)

## Legal

Offered under the [Apache 2 license][license].

[license]: LICENSE
[buf]: https://buf.build
[buf-mod]: https://buf.build/bufbuild/protovalidate
[cel-spec]: https://github.com/google/cel-spec
