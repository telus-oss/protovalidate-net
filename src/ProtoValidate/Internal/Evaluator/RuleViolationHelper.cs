using Buf.Validate;

namespace ProtoValidate.Internal.Evaluator;

internal class RuleViolationHelper
{
    public FieldPath? RulePrefix { get; }
    public FieldPathElement? FieldPathElement { get; }
    public RuleViolationHelper(ValueEvaluator? valueEvaluator)
    {
        if (valueEvaluator != null)
        {
            RulePrefix = valueEvaluator.NestedRule;
            FieldPathElement = valueEvaluator.FieldPathElement;
        }
        else
        {
            RulePrefix = null;
            FieldPathElement = null;
        }
    }

    public List<FieldPathElement> RulePrefixElements
    {
        get
        {
            if (RulePrefix == null)
            {
                return [];
            }
            return RulePrefix.Elements.ToList();
        }
    }
}