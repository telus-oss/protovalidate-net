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

            Console.WriteLine($"Evaluating rule '{compiledProgram.Source.Id}': {compiledProgram.Source.ExpressionText}");

            var violation = compiledProgram.Eval(variables);
            if (violation != null)
            {
                Console.WriteLine($"  Rule found violation: {violation}");
                violationList.Add(violation);
            }
            else
            {
                Console.WriteLine($"  Rule has no violations.");
            }
            Console.WriteLine();

            if (failFast)
            {
                break;
            }
        }

        return new ValidationResult(violationList);
    }
}