using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class ListEvaluator : IEvaluator
{
    public ValueEvaluator ItemConstraints { get; }
    public FieldDescriptor FieldDescriptor { get; }
    public FieldConstraints FieldConstraints { get; }


    public ListEvaluator(FieldConstraints fieldConstraints, FieldDescriptor fieldDescriptor)
    {
        if (fieldConstraints == null)
        {
            throw new ArgumentNullException(nameof(fieldConstraints));
        }

        if (fieldDescriptor == null)
        {
            throw new ArgumentNullException(nameof(fieldDescriptor));
        }

        FieldConstraints = fieldConstraints;
        FieldDescriptor = fieldDescriptor;


        ItemConstraints = new ValueEvaluator(fieldConstraints, fieldDescriptor);
    }
    public override string ToString()
    {
        return $"List Evaluator: {FieldDescriptor.FullName}";
    }

    public bool Tautology => ItemConstraints.Tautology;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (value == null)
        {
            return ValidationResult.Empty;
        }

        var allViolations = new List<Violation>();

        var repeatedValues = value.RepeatedValue();

        for (var i = 0; i < repeatedValues.Count; i++)
        {
            var evalResult = ItemConstraints.Evaluate(repeatedValues[i], failFast);
            if (evalResult.Violations.Count == 0)
            {
                continue;
            }

            var violations = evalResult.Violations.PrefixErrorPaths("[{0}]", i);
            if (failFast && violations.Count > 0)
            {
                return evalResult;
            }

            allViolations.AddRange(violations);
        }

        return new ValidationResult(allViolations);
    }
}