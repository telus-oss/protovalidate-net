using Buf.Validate;
using ProtoValidate.Internal.Evaluator;

namespace ProtoValidate.Internal.Cel;

public class CompiledProgramsEvaluator : IEvaluator
{
    private List<CompiledProgram> CompiledPrograms { get; }

    public CompiledProgramsEvaluator(List<CompiledProgram> compiledPrograms)
    {
        CompiledPrograms = compiledPrograms ?? throw new ArgumentNullException(nameof(compiledPrograms));
    }

    public bool Tautology => CompiledPrograms.Count == 0;

    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var variables = new Dictionary<string, object?>();
        variables.Add("this", value?.Value<object?>());
        variables.Add("now", DateTimeOffset.UtcNow);

        var violationList = new List<Violation>();

        foreach (var compiledProgram in CompiledPrograms)
        {

            Console.WriteLine($"Evaluating program: {compiledProgram.Source.ExpressionText}");

            var violation = compiledProgram.Eval(variables);
            if (violation != null)
            {
                violationList.Add(violation);
            }

            if (failFast)
            {
                break;
            }
        }

        return new ValidationResult(violationList);
    }
}