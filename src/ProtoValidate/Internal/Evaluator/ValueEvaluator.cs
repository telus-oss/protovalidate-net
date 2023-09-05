using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class ValueEvaluator : IEvaluator
{
    public FieldConstraints FieldConstraints { get; }
    public FieldDescriptor FieldDescriptor { get; }
    public List<IEvaluator> Evaluators { get; } = new();
    public ValueEvaluator(FieldConstraints fieldConstraints, FieldDescriptor fieldDescriptor)
    {
        FieldConstraints = fieldConstraints ?? throw new ArgumentNullException(nameof(fieldConstraints));
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
    }


    /// <summary>
    ///     Indicates that the Constraints should not be applied if the field is unset or the default
    ///     (typically zero) value.
    /// </summary>
    public bool IgnoreEmpty => FieldConstraints.IgnoreEmpty;

    public override string ToString()
    {
        return $"Value Evaluator: {FieldDescriptor.FullName}";
    }

    public void AddEvaluator(IEvaluator evaluator)
    {
        if (evaluator == null)
        {
            throw new ArgumentNullException(nameof(evaluator));
        }

        Evaluators.Add(evaluator);
    }


    public bool Tautology => Evaluators.Count > 0;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        if (IgnoreEmpty)
        {
            if (value == null)
            {
                return ValidationResult.Empty;
            }

            if (IsZero(value.Value<object?>()))
            {
                return ValidationResult.Empty;
            }
        }

        var violations = new List<Violation>();
        foreach (var evaluator in Evaluators)
        {
            var evalResult = evaluator.Evaluate(value, failFast);

            if (failFast && !evalResult.IsSuccess)
            {
                return evalResult;
            }

            violations.AddRange(evalResult.Violations);
        }

        if (violations.Count == 0)
        {
            return ValidationResult.Empty;
        }

        return new ValidationResult(violations);
    }
    private bool IsZero(object? val)
    {
        if (val == null)
        {
            return false;
        }

        try
        {
            if (ValueEquality(val, 0) || ValueEquality(val, 0.0))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static bool ValueEquality(object val1, object val2)
    {
        if (!(val1 is IConvertible)) return false;
        if (!(val2 is IConvertible)) return false;

        // convert val2 to type of val1.
        var converted2 = Convert.ChangeType(val2, val1.GetType());

        // compare now that same type.
        return val1.Equals(converted2);
    }
}