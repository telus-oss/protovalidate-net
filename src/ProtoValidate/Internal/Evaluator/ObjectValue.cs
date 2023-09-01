using System.Collections;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class ObjectValue : IValue
{
    private object? InternalValue { get; }
    private FieldDescriptor FieldDescriptor { get; }

    public ObjectValue(FieldDescriptor fieldDescriptor, object? internalValue)
    {
        FieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
        InternalValue = internalValue;
    }


    public IMessage? MessageValue
    {
        get
        {
            if (FieldDescriptor.FieldType == FieldType.Message)
            {
                return (IMessage?)InternalValue;
            }

            return null;
        }
    }

    public T? Value<T>() where T : class?
    {
        return (T?)InternalValue;
    }

    public List<IValue> RepeatedValue()
    {
        var list = new List<IValue>();

        if (FieldDescriptor.IsRepeated && InternalValue is IEnumerable internalValueList)
        {
            foreach (var value in internalValueList)
            {
                list.Add(new ObjectValue(FieldDescriptor, value));
            }
        }

        return list;
    }

    public Dictionary<IValue, IValue> MapValue()
    {
        var dict = new Dictionary<IValue, IValue>();

        if (FieldDescriptor.IsMap && InternalValue is IDictionary internalValueDictionary)
        {
            var keyDescriptor = FieldDescriptor.MessageType.FindFieldByNumber(1);
            var valueDescriptor = FieldDescriptor.MessageType.FindFieldByNumber(2);

            foreach (DictionaryEntry entry in internalValueDictionary)
            {
                var keyValue = new ObjectValue(keyDescriptor, entry.Key);
                var valueValue = new ObjectValue(valueDescriptor, entry.Value);

                dict.Add(keyValue, valueValue);
            }
        }

        return dict;
    }
}