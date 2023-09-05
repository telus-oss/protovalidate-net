using Cel;
using Cel.Internal;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProtoValidate.Internal.Cel;
using ProtoValidate.Internal.Evaluator;
using ProtoValidate.Internal.Evaluator.Evaluator;

namespace ProtoValidate;

public class Validator
{
    private EvaluatorBuilder EvaluatorBuilder { get; }
    private bool FailFast { get; }

    public Validator() : this(new Config())
    {

    }

    public Validator(Config config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        FailFast = config.FailFast;
        var functions = new Dictionary<string, CelFunctionDelegate>();


        FileDescriptor[] fileDescriptors = Array.Empty<FileDescriptor>();

        var celEnvironment = new CelEnvironment(fileDescriptors, "");
        celEnvironment.StrictTypeComparison = true;
        celEnvironment.RegisterProtoValidateFunctions();
        celEnvironment.RegisterProtoValidateFormatFunction();


        EvaluatorBuilder = new EvaluatorBuilder(celEnvironment, config.DisableLazy);
    }

    public ValidationResult Validate(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var descriptor = message.Descriptor;
        var evaluator = EvaluatorBuilder.Load(descriptor);

        return evaluator.Evaluate(new MessageValue(message), FailFast);
    }

    public void LoadDescriptors(IEnumerable<MessageDescriptor> descriptors)
    {
        if (descriptors == null)
        {
            throw new ArgumentNullException(nameof(descriptors));
        }
        foreach (var descriptor in descriptors)
        {
            EvaluatorBuilder.Load(descriptor);
        }
    }

    public void LoadMessages(IEnumerable<IMessage> messages)
    {
        if (messages == null)
        {
            throw new ArgumentNullException(nameof(messages));
        }

        var descriptors = messages.Select(c => c.Descriptor).Distinct();
        LoadDescriptors(descriptors);
    }

    private string GetEvaluatorDebugString(IEvaluator evaluator, int nestLevel, List<IEvaluator> visitedEvaluators)
    {
        if (visitedEvaluators.Contains(evaluator))
        {
            return new string(' ', nestLevel * 4) + evaluator + " (Nested)" + Environment.NewLine;

        }

        visitedEvaluators.Add(evaluator);

        var sb = new System.Text.StringBuilder();
        sb.Append(new string(' ', nestLevel * 4)).AppendLine(evaluator.ToString());

        if (evaluator is MessageEvaluator messageEvaluator)
        {
            foreach (var subEvaluator in messageEvaluator.Evaluators)
            {
                sb.Append(GetEvaluatorDebugString(subEvaluator, nestLevel + 1, visitedEvaluators));
            }

        }
        else if (evaluator is AnyEvaluator anyEvaluator)
        {

        }

        else if (evaluator is EnumEvaluator enumEvaluator)
        {

        }
        else if (evaluator is FieldEvaluator fieldEvaluator)
        {
            sb.Append(GetEvaluatorDebugString(fieldEvaluator.ValueEvaluator, nestLevel + 1, visitedEvaluators));
        }
        else if (evaluator is ListEvaluator listEvaluator)
        {
            sb.Append(GetEvaluatorDebugString(listEvaluator.ItemConstraints, nestLevel + 1, visitedEvaluators));
        }
        else if (evaluator is OneofEvaluator oneofEvaluator)
        {

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
        else if (evaluator is UnknownDescriptorEvaluator unknownDescriptorEvaluator)
        {

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