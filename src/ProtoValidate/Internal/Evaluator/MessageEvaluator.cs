using Buf.Validate;

namespace ProtoValidate.Internal.Evaluator;

public class MessageEvaluator : IEvaluator
{
    private List<IEvaluator> Evaluators { get; } = new();

    public void AddEvaluator(IEvaluator evaluator)
    {
        if (evaluator == null)
        {
            throw new ArgumentNullException(nameof(evaluator));
        }

        Evaluators.Add(evaluator);
    }

    public bool Tautology
    {
        get
        {
            foreach (var evaluator in Evaluators)
            {
                if (!evaluator.Tautology)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var violations = new List<Violation>();
        foreach (var evaluator in Evaluators)
        {
            var evalResult = evaluator.Evaluate(value, failFast);
            if (failFast && evalResult.Violations.Count > 0)
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
}