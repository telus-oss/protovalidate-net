using Antlr4.Runtime.Misc;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace ProtoValidate.Internal.Evaluator;

public interface IValue
{
    IMessage? MessageValue { get; }
    T? Value<T>() where T : class?;
    List<IValue> RepeatedValue();
    Dictionary<IValue, IValue> MapValue();
}