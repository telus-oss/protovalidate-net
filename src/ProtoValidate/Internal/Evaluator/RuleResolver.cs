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

using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

internal class RuleResolver
{
    /// <summary>
    ///     Resolves the rules for a message descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor for the message.</param>
    /// <returns>Returns the resolved message rules.</returns>
    public MessageRules ResolveMessageRules(MessageDescriptor descriptor)
    {
        var messageOptions = descriptor.GetOptions();
        if (messageOptions == null)
        {
            return new MessageRules();
        }

        var messageExtension = ValidateExtensions.Message;

        if (!messageOptions.HasExtension(messageExtension))
        {
            return new MessageRules();
        }

        var messageRules = messageOptions.GetExtension(messageExtension);

        if (messageRules == null)
        {
            return new MessageRules();
        }
        
        return messageRules;
    }

    public OneofRules ResolveOneofRules(OneofDescriptor descriptor)
    {
        var options = descriptor.GetOptions();
        if (options == null)
        {
            return new OneofRules();
        }

        if (!options.HasExtension(ValidateExtensions.Oneof))
        {
            return new OneofRules();
        }

        return options.GetExtension(ValidateExtensions.Oneof);
    }
    
    public FieldRules ResolveFieldRules(FieldDescriptor descriptor)
    {
        var options = descriptor.GetOptions();
        if (options == null || !options.HasExtension(ValidateExtensions.Field))
        {
            return new FieldRules();
        }

        var fieldRules = options.GetExtension(ValidateExtensions.Field);

        if (fieldRules.Ignore == Ignore.Unspecified)
        {
            if (descriptor.HasPresence)
            {
                fieldRules.Ignore = Ignore.IfZeroValue;
            }
        }

        return fieldRules;
    }

    // public PredefinedRules ResolvePredefinedRules(FieldDescriptor descriptor)
    // {
    //     var options = descriptor.GetOptions();
    //     if (options == null || !options.HasExtension(ValidateExtensions.Predefined))
    //     {
    //         return new PredefinedRules();
    //     }
    //
    //     var predefinedRules = options.GetExtension(ValidateExtensions.Predefined);
    //     return predefinedRules;
    // }
}