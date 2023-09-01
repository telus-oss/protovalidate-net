using Buf.Validate;
using Cel;
using Cel.Internal;
using Google.Protobuf;
using ProtoValidate.Exceptions;

namespace ProtoValidate.Internal.Cel;

public class CompiledProgram
{
    public CelProgramDelegate CelProgramDelegate { get; }
    public Expression Source { get; }
    public IMessage? Rules { get; }

    public CompiledProgram(CelProgramDelegate celExpressionDelegate, IMessage? rules, Expression source)
    {
        CelProgramDelegate = celExpressionDelegate ?? throw new ArgumentNullException(nameof(celExpressionDelegate));
        Rules = rules;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Violation? Eval(IDictionary<string, object?> variables)
    {
        object? evalResult;

        try
        {
            //set the rules
            if (Rules != null)
            {
                variables["rules"] = Rules;
            }
            else
            {
                variables.Remove("rules");
            }
            
            evalResult = CelProgramDelegate?.Invoke(variables);
        }
        catch (Exception x)
        {
            throw new ExecutionException($"Error evaluating {Source.Id}", x, Source);
        }

        if (evalResult is string evalResultString)
        {
            if (string.IsNullOrWhiteSpace(evalResultString))
            {
                return null;
            }

            return new Violation
            {
                ConstraintId = Source.Id,
                Message = evalResultString
            };
        }

        if (evalResult is bool evalResultBool)
        {
            if (evalResultBool)
            {
                return null;
            }

            return new Violation
            {
                ConstraintId = Source.Id,
                Message = Source.Message
            };
        }

        throw new ExecutionException($"Resolved to an unexpected type {evalResult?.GetType().FullName ?? "null"}", Source);
    }
}