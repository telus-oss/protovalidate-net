using Google.Protobuf;

namespace ProtoValidate.Internal.Evaluator;

public class MessageValue : IValue
{
    private IMessage InternalValue { get; }

    public MessageValue(IMessage value)
    {
        InternalValue = value ?? throw new ArgumentNullException(nameof(value));
    }

    IMessage? IValue.MessageValue => InternalValue;

    public Dictionary<IValue, IValue> MapValue()
    {
        return new Dictionary<IValue, IValue>();
    }

    public List<IValue> RepeatedValue()
    {
        return new List<IValue>();
    }

    public T Value<T>() where T : class?
    {
        return (T)InternalValue;
    }
}