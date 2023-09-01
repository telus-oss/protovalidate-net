namespace ProtoValidate.Internal.Evaluator;

public interface IEvaluator
{
    bool Tautology { get; }
    ValidationResult Evaluate(IValue? value, bool failFast);
}