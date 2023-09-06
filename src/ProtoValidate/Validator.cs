using System.Text;
using Cel;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Options;
using ProtoValidate.Internal.Cel;
using ProtoValidate.Internal.Evaluator;
using ProtoValidate.Internal.Evaluator.Evaluator;

namespace ProtoValidate;

public class Validator : IValidator
{
    private ValidatorOptions Options { get; }

    private EvaluatorBuilder? EvaluatorBuilder { get; set; }

    public Validator() : this(new ValidatorOptions()) { }

    public Validator(ValidatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        Options = options;
        Initialize();
    }

    public Validator(IOptions<ValidatorOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor, nameof(optionsAccessor));

        Options = optionsAccessor.Value;
        Initialize();
    }

    public ValidationResult Validate(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var descriptor = message.Descriptor;
        var evaluator = EvaluatorBuilder!.Load(descriptor);

        return evaluator.Evaluate(new MessageValue(message), Options.FailFast);
    }

    private void Initialize()
    {
        var fileDescriptorList = Options.FileDescriptors ?? new List<FileDescriptor>();

        var celEnvironment = new CelEnvironment(fileDescriptorList, "");
        celEnvironment.StrictTypeComparison = true;
        celEnvironment.RegisterProtoValidateFunctions();
        celEnvironment.RegisterProtoValidateFormatFunction();

        EvaluatorBuilder = new EvaluatorBuilder(celEnvironment, Options.DisableLazy);

        if (Options.PreLoadDescriptors)
        {
            foreach (var fileDescriptor in fileDescriptorList)
            {
                foreach (var messageDescriptor in fileDescriptor.MessageTypes)
                {
                    EvaluatorBuilder.Load(messageDescriptor);

                    foreach (var nestedTypeMessageDescriptor in messageDescriptor.NestedTypes)
                    {
                        EvaluatorBuilder.Load(nestedTypeMessageDescriptor);
                    }
                }
            }
        }
    }

    public string GetEvaluatorDebugString(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var descriptor = message.Descriptor;
        var evaluator = EvaluatorBuilder!.Load(descriptor);
        return GetEvaluatorDebugString(evaluator, 0, new List<IEvaluator>());
    }

    private string GetEvaluatorDebugString(IEvaluator evaluator, int nestLevel, List<IEvaluator> visitedEvaluators)
    {
        if (visitedEvaluators.Contains(evaluator))
        {
            return new string(' ', nestLevel * 4) + evaluator + " (Nested)" + Environment.NewLine;
        }

        visitedEvaluators.Add(evaluator);

        var sb = new StringBuilder();
        sb.Append(new string(' ', nestLevel * 4)).AppendLine(evaluator.ToString());

        if (evaluator is MessageEvaluator messageEvaluator)
        {
            foreach (var subEvaluator in messageEvaluator.Evaluators)
            {
                sb.Append(GetEvaluatorDebugString(subEvaluator, nestLevel + 1, visitedEvaluators));
            }
        }
        else if (evaluator is FieldEvaluator fieldEvaluator)
        {
            sb.Append(GetEvaluatorDebugString(fieldEvaluator.ValueEvaluator, nestLevel + 1, visitedEvaluators));
        }
        else if (evaluator is ListEvaluator listEvaluator)
        {
            sb.Append(GetEvaluatorDebugString(listEvaluator.ItemConstraints, nestLevel + 1, visitedEvaluators));
        }
        else if (evaluator is MapEvaluator mapEvaluator)
        {
            sb.Append(GetEvaluatorDebugString(mapEvaluator.KeyEvaluator, nestLevel + 1, visitedEvaluators));
            sb.Append(GetEvaluatorDebugString(mapEvaluator.ValueEvaluator, nestLevel + 1, visitedEvaluators));
        }
        else if (evaluator is ValueEvaluator valueEvaluator)
        {
            foreach (var subEvaluator in valueEvaluator.Evaluators)
            {
                sb.Append(GetEvaluatorDebugString(subEvaluator, nestLevel + 1, visitedEvaluators));
            }
        }
        else if (evaluator is CompiledProgramsEvaluator compiledProgramsEvaluator)
        {
            foreach (var compiledProgram in compiledProgramsEvaluator.CompiledPrograms)
            {
                sb.AppendLine(new string(' ', (nestLevel + 1) * 4) + compiledProgram.Source.Id + " - " + compiledProgram.Source.ExpressionText);
            }
        }

        return sb.ToString();
    }
}