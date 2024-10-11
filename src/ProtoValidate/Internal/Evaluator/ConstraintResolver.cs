// Copyright 2023 TELUS
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

using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class ConstraintResolver
{
    /// <summary>
    ///     Resolves the constraints for a message descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor for the message.</param>
    /// <returns>Returns the resolved message constraints.</returns>
    public MessageConstraints ResolveMessageConstraints(MessageDescriptor descriptor)
    {
        var messageOptions = descriptor.GetOptions();
        if (messageOptions == null)
        {
            return new MessageConstraints();
        }

        var messageExtension = ValidateExtensions.Message;

        if (!messageOptions.HasExtension(messageExtension))
        {
            return new MessageConstraints();
        }

        var messageConstraints = messageOptions.GetExtension(messageExtension);

        if (messageConstraints == null)
        {
            return new MessageConstraints();
        }

        var disabled = messageConstraints.Disabled;
        if (disabled)
        {
            return new MessageConstraints
            {
                Disabled = true
            };
        }

        return messageConstraints;
    }

    public OneofConstraints ResolveOneofConstraints(OneofDescriptor descriptor)
    {
        var options = descriptor.GetOptions();
        if (options == null)
        {
            return new OneofConstraints();
        }

        if (!options.HasExtension(ValidateExtensions.Oneof))
        {
            return new OneofConstraints();
        }

        return options.GetExtension(ValidateExtensions.Oneof);
    }

    public FieldConstraints ResolveFieldConstraints(FieldDescriptor descriptor)
    {
        var options = descriptor.GetOptions();
        if (options == null || !options.HasExtension(ValidateExtensions.Field))
        {
            return new FieldConstraints();
        }

        var fieldConstraints = options.GetExtension(ValidateExtensions.Field);

        return fieldConstraints;
    }
}