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

namespace ProtoValidate.Internal.Cel;

public class Expression
{
    /// <summary>The id of the rule</summary>
    public string Id { get; }

    /// <summary>The message of the rule</summary>
    public string Message { get; }

    /// <summary>The CEL Expression of the rule of the rule</summary>
    public string ExpressionText { get; }


    /// <summary>
    ///     Constructs a new expression
    /// </summary>
    /// <param name="id">The id of the rule</param>
    /// <param name="message">The message of the rule</param>
    /// <param name="expressionText">The CEL Expression of the rule of the rule</param>
    public Expression(string id, string message, string expressionText)
    {
        Id = id;
        Message = message;
        ExpressionText = expressionText;
    }

    public Expression(Rule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        Id = rule.Id;
        Message = rule.Message;
        ExpressionText = rule.Expression;
    }
    
    public static IEnumerable<Expression> FromRules(IEnumerable<Rule> rules)
    {
        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        foreach (var rule in rules)
        {
            yield return new Expression(rule);
        }
    }
}