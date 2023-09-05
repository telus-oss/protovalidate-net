using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class OneofEvaluator : IEvaluator
{
    private OneofDescriptor Descriptor { get; }
    private bool Required { get; }

    public OneofEvaluator(OneofDescriptor descriptor, bool required)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Required = required;
    }

    public override string ToString()
    {
        return $"OneOf Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology => !Required;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var message = value.MessageValue;
        if (message == null)
        {
            return ValidationResult.Empty;
        }

        var caseFieldDescriptor = Descriptor.Accessor.GetCaseFieldDescriptor(message);

        if (Required && caseFieldDescriptor == null)
        {
            return new ValidationResult(new[]
            {
                new Violation
                {
                    ConstraintId = "required",
                    Message = "Exactly one field is required in oneof."
                }
            });
        }

        return ValidationResult.Empty;
    }
}