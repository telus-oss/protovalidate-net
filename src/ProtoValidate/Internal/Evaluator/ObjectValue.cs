// Copyright 2023-2025 TELUS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

internal class ObjectValue : IValue
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

            foreach (var entry in internalValueDictionary)
            {
                if (entry == null)
                {
                    continue;
                }

                var entryValue = (DictionaryEntry)entry;
                var keyValue = new ObjectValue(keyDescriptor, entryValue.Key);
                var valueValue = new ObjectValue(valueDescriptor, entryValue.Value);

                dict.Add(keyValue, valueValue);
            }
        }

        return dict;
    }
}