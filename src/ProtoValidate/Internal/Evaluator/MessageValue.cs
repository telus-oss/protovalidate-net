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

using Google.Protobuf;

namespace ProtoValidate.Internal.Evaluator;

internal class MessageValue : IValue
{
    internal IMessage InternalValue { get; }

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