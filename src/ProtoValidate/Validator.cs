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

        Functions.RegisterFunctions(functions);
        FormatFunction.RegisterFunctions(functions);
        
        FileDescriptor[] fileDescriptors = Array.Empty<FileDescriptor>();

        var celEnvironment = new CelEnvironment(functions, fileDescriptors, "");
        celEnvironment.StrictTypeComparison = true;

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
}