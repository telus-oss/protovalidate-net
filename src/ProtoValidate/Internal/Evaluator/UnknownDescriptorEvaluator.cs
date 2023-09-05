using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class UnknownDescriptorEvaluator : IEvaluator
{
    private DescriptorBase Descriptor { get; }

    public UnknownDescriptorEvaluator(DescriptorBase descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        Descriptor = descriptor;
    }

    public override string ToString()
    {
        return $"UnknownDescriptorEvaluator Evaluator: {Descriptor.FullName}";
    }

    public bool Tautology => false;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ValidationResult(new[]
        {
            new Violation
            {
                Message = $"No evaluator available for {Descriptor.FullName}."
            }
        });
    }
}