using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class EnumEvaluator : IEvaluator
{
    private IList<EnumValueDescriptor> ValueDescriptors { get; }
    private Dictionary<int, EnumValueDescriptor> Values { get; }

    public EnumEvaluator(IList<EnumValueDescriptor> valueDescriptors)
    {
        if (valueDescriptors == null)
        {
            throw new ArgumentNullException(nameof(valueDescriptors));
        }

        ValueDescriptors = valueDescriptors;
        Values = new Dictionary<int, EnumValueDescriptor>();

        foreach (var descriptor in valueDescriptors)
        {
            Values[descriptor.Number] = descriptor;
        }
    }

    public override string ToString()
    {
        return "Enum Evaluator";
    }

    public bool Tautology => false;

    /// <summary>
    /// Evaluates an enum value.
    /// </summary>
    /// <param name="value">The value to evaluate</param>
    /// <param name="failFast">Indicates if the evaluation should stop on the first violation.</param>
    /// <returns></returns>
    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var enumValue = value?.Value<object?>();
        if (enumValue == null)
        {
            return ValidationResult.Empty;
        }

        var enumIntValue = (int)enumValue;

        if (!Values.ContainsKey(enumIntValue))
        {
            return new ValidationResult(new[]
            {
                new Violation
                {
                    ConstraintId = "enum.defined_only",
                    Message = "Value must be one of the defined enum values."
                }
            });
        }

        return ValidationResult.Empty;
    }
}