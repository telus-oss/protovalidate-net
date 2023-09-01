using System.Collections;
using Buf.Validate;
using Google.Protobuf.Reflection;

namespace ProtoValidate.Internal.Evaluator;

public class FieldEvaluator : IEvaluator
{
    public ValueEvaluator ValueEvaluator { get; }
    private FieldDescriptor Descriptor { get; }


    /// <summary>
    ///     Indicates that the field must have a set value
    /// </summary>
    private bool Required { get; }

    /// <summary>
    ///     Indicates that the evaluators should not be applied to this field if the value is unset. Fields
    ///     that contain messages, are prefixed with `optional`, or are part of a oneof are considered
    ///     optional. evaluators will still be applied if the field is set as the zero value.
    /// </summary>
    private bool Optional { get; }

    public FieldEvaluator(ValueEvaluator valueEvaluator, FieldDescriptor descriptor, bool required, bool optional)
    {
        ValueEvaluator = valueEvaluator ?? throw new ArgumentNullException(nameof(valueEvaluator));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Required = required;
        Optional = optional;
    }

    public bool Tautology => !Required && ValueEvaluator.Tautology;


    public ValidationResult Evaluate(IValue? value, bool failFast)
    {
        var message = value?.MessageValue;
        if (message == null)
        {
            return ValidationResult.Empty;
        }

        bool hasField;
        if (Descriptor.IsMap)
        {
            var list = (IDictionary)Descriptor.Accessor.GetValue(message);
            hasField = list.Count > 0;
        }
        else if (Descriptor.IsRepeated)
        {
            var list = (IList)Descriptor.Accessor.GetValue(message);
            hasField = list.Count > 0;
        }
        else if (Descriptor.HasPresence)
        {
            hasField = Descriptor.Accessor.HasValue(message);
        }
        else
        {
            hasField = true;
        }

        if (Required && !hasField)
        {
            return new ValidationResult(new[]
            {
                new Violation
                {
                    ConstraintId = "required",
                    Message = "Value is required."
                }
            });
        }

        if ((Optional || ValueEvaluator.IgnoreEmpty) && !hasField)
        {
            return ValidationResult.Empty;
        }

        var fieldValue = Descriptor.Accessor.GetValue(message);

        var evalResult = ValueEvaluator.Evaluate(new ObjectValue(Descriptor, fieldValue), failFast);
        var violations = evalResult.Violations.PrefixErrorPaths("{0}", Descriptor.Name);

        return new ValidationResult(violations);
    }
}