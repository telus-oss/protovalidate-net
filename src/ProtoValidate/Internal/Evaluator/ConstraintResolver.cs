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

        return options.GetExtension(ValidateExtensions.Field);
    }
}