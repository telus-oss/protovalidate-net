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

namespace ProtoValidate.Internal.Cel;

public class Expression
{
    /// <summary>The id of the constraint</summary>
    public string Id { get; }

    /// <summary>The message of the constraint</summary>
    public string Message { get; }

    /// <summary>The CEL Expression of the constraint of the constraint</summary>
    public string ExpressionText { get; }


    /// <summary>
    ///     Constructs a new expression
    /// </summary>
    /// <param name="id">The id of the constraint</param>
    /// <param name="message">The message of the constraint</param>
    /// <param name="expressionText">The CEL Expression of the constraint of the constraint</param>
    public Expression(string id, string message, string expressionText)
    {
        Id = id;
        Message = message;
        ExpressionText = expressionText;
    }

    public Expression(Constraint constraint)
    {
        if (constraint == null)
        {
            throw new ArgumentNullException(nameof(constraint));
        }

        Id = constraint.Id;
        Message = constraint.Message;
        ExpressionText = constraint.Expression;
    }
    
    public static IEnumerable<Expression> FromConstraints(IEnumerable<Constraint> constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        foreach (var constraint in constraints)
        {
            yield return new Expression(constraint);
        }
    }
}