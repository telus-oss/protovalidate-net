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

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Buf.Validate;
using Cel;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProtoValidate.Exceptions;
using ProtoValidate.Internal.Cel;
using FieldConstraints = Buf.Validate.FieldConstraints;

namespace ProtoValidate.Internal.Constraints;

public class ConstraintCache
{
    public ConstraintCache(CelEnvironment celEnvironment, IList<FieldDescriptor> extensions)
    {
        if (celEnvironment == null)
        {
            throw new ArgumentNullException(nameof(celEnvironment));
        }

        if (extensions == null)
        {
            throw new ArgumentNullException(nameof(extensions));
        }

        CelEnvironment = celEnvironment;
        Extensions = extensions;
    }

    /// <summary>
    ///     Map for caching descriptor and their expression delegates
    /// </summary>
    private ConcurrentDictionary<FieldDescriptor, List<CompiledExpression>> DescriptorMap { get; } = new();

    private CelEnvironment CelEnvironment { get; }
    private IList<FieldDescriptor> Extensions { get; }

    public List<CompiledProgram> Compile(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems)
    {
        var compiledProgramList = new List<CompiledProgram>();

        var rulesMessage = ResolveConstraints(fieldDescriptor, fieldConstraints, forItems);
        if (rulesMessage == null)
        {
            // Message null means there were no constraints resolved.
            return new List<CompiledProgram>();
        }

        //build a cache of all possible constraints for this descriptor field
        foreach (var constraintFieldDescriptor in rulesMessage.Descriptor.Fields.InDeclarationOrder())
        {
            if (!DescriptorMap.ContainsKey(constraintFieldDescriptor))
            {
                var options = constraintFieldDescriptor.GetOptions();

                if (options == null)
                {
                    continue;
                }

                // var constraints = options.GetExtension(ValidateExtensions.Predefined);
                //
                // if (constraints == null)
                // {
                //     constraints = new PredefinedConstraints();
                // }
                //var constraints = new PredefinedConstraints();
                //fix this - pulled from fieldConstraints.Cel
                var expressions = Expression.FromConstraints(fieldConstraints.Cel).ToList();

                var compiledPrograms = new List<CompiledExpression>();

                foreach (var expression in expressions)
                {
                    var celExpression = CelEnvironment.Compile(expression.ExpressionText);

                    var compiledExpressionItem = new CompiledExpression(expression, celExpression);
                    compiledPrograms.Add(compiledExpressionItem);
                }

                DescriptorMap[constraintFieldDescriptor] = compiledPrograms;
            }
        }

        //this is where we would compile the extensions, if there are any.
        var ruleType = rulesMessage.GetType();

        foreach (var extensionFieldDescriptor in Extensions.Where(c => c.IsExtension && c.ExtendeeType.ClrType == ruleType))
        {
            var extensionOptions = extensionFieldDescriptor.GetOptions();
            // var predefinedConstraints = extensionOptions.GetExtension(ValidateExtensions.Predefined);
            // if (predefinedConstraints == null)
            // {
            //     continue;
            // }

            var extension = extensionFieldDescriptor.Extension;
            var extensionType = extension.GetType();
            var extensionTypeGenericArguments = extensionType.GetGenericArguments();
            var extensionValueType = extensionTypeGenericArguments[1];
            var extendableMessageType = typeof(IExtendableMessage<>).MakeGenericType(ruleType);

            object? extensionValue = null;

            if (extensionType.GetGenericTypeDefinition() == typeof(Google.Protobuf.RepeatedExtension<,>))
            {
                //we have a repeated extension
                var getExtensionValueMethod = extendableMessageType.GetMethods().FirstOrDefault(c => c.Name == "GetExtension" && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extensionType.GetGenericTypeDefinition());
                if (getExtensionValueMethod == null)
                {
                    throw new ValidationException($"GetExtension method not found on type {extendableMessageType.FullName}.  Google may have changed the interface of Google.Protobuf library.");
                }

                var genericGetExtensionValueMethod = getExtensionValueMethod.MakeGenericMethod(extensionValueType);

                extensionValue = genericGetExtensionValueMethod.Invoke(rulesMessage, new object?[] { extension });
                if (extensionValue == null)
                {
                    continue;
                }
            }
            else if (extensionType.GetGenericTypeDefinition() == typeof(Google.Protobuf.Extension<,>))
            {
                //we have a single-value extension
                var hasExtensionMethod = extendableMessageType.GetMethod("HasExtension", BindingFlags.Instance | BindingFlags.Public);
                if (hasExtensionMethod == null)
                {
                    throw new ValidationException($"HasExtension method not found on type {extendableMessageType.FullName}.  Google may have changed the interface of Google.Protobuf library.");
                }

                var genericHasExtensionMethod = hasExtensionMethod.MakeGenericMethod(new System.Type[] { extensionValueType });

                var hasExtension = (bool?)genericHasExtensionMethod.Invoke(rulesMessage, new object?[] { extension }) ?? false;
                if (!hasExtension)
                {
                    continue;
                }

                var getExtensionValueMethod = extendableMessageType.GetMethods().FirstOrDefault(c => c.Name == "GetExtension" && c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extensionType.GetGenericTypeDefinition());
                if (getExtensionValueMethod == null)
                {
                    throw new ValidationException($"GetExtension method not found on type {extendableMessageType.FullName}.  Google may have changed the interface of Google.Protobuf library.");
                }

                var genericGetExtensionValueMethod = getExtensionValueMethod.MakeGenericMethod(extensionValueType);

                extensionValue = genericGetExtensionValueMethod.Invoke(rulesMessage, new object?[] { extension });
            }

            //FIX
            //var expressions = Expression.FromConstraints(predefinedConstraints.Cel).ToList();
            var expressions = Expression.FromConstraints(fieldConstraints.Cel).ToList();

            foreach (var expression in expressions)
            {
                var celExpression = CelEnvironment.Compile(expression.ExpressionText);

                var compiledProgram = new CompiledProgram(celExpression, null, extensionValue, expression);
                compiledProgramList.Add(compiledProgram);
            }
        }

        //now check to see if we need to add the constraint at all based on the specific rules for this field.
        foreach (var constraintFieldDescriptor in rulesMessage.Descriptor.Fields.InDeclarationOrder())
        {
            if (constraintFieldDescriptor.HasPresence)
            {
                var hasRule = constraintFieldDescriptor.Accessor.HasValue(rulesMessage);
                if (!hasRule)
                {
                    continue;
                }
            }
            else if (constraintFieldDescriptor.IsMap)
            {
                var value = (IDictionary)constraintFieldDescriptor.Accessor.GetValue(rulesMessage);
                if (value.Count == 0)
                {
                    continue;
                }
            }
            else if (constraintFieldDescriptor.IsRepeated)
            {
                var value = (IList)constraintFieldDescriptor.Accessor.GetValue(rulesMessage);
                if (value.Count == 0)
                {
                    continue;
                }
            }
            else if (constraintFieldDescriptor.FieldType == FieldType.Bool)
            {
                var value = (bool)constraintFieldDescriptor.Accessor.GetValue(rulesMessage);
                if (!value)
                {
                    continue;
                }
            }

            if (DescriptorMap.TryGetValue(constraintFieldDescriptor, out var compiledExpressionList))
            {
                var compiledPrograms = compiledExpressionList.Select(c => new CompiledProgram(c.CelProgramDelegate, rulesMessage, null, c.Source)).ToList();
                compiledProgramList.AddRange(compiledPrograms);
            }
        }

        return compiledProgramList;
    }

    private IMessage? ResolveConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems)
    {
        var fieldOneofs = FieldConstraints.Descriptor.Oneofs;
        if (fieldOneofs == null || fieldOneofs.Count == 0)
        {
            // If the oneof field descriptor is null there are no constraints to resolve.
            return null;
        }

        var oneofFieldDescriptor = fieldOneofs[0].Accessor.GetCaseFieldDescriptor(fieldConstraints);
        if (oneofFieldDescriptor == null)
        {
            return null;
        }

        // Get the expected constraint descriptor based on the provided field descriptor and the flag
        // indicating whether it is for items.
        var expectedConstraintDescriptor = DescriptorMappings.GetExpectedConstraintDescriptor(fieldDescriptor, forItems);
        if (expectedConstraintDescriptor == null)
        {
            return null;
        }

        if (oneofFieldDescriptor.FullName != expectedConstraintDescriptor.FullName)
        {
            // If the expected constraint does not match the actual oneof constraint, throw a
            // CompilationError.
            throw new CompilationException($"Expected constraint '{expectedConstraintDescriptor.FullName}', got '{oneofFieldDescriptor.FullName}' on field '{fieldDescriptor.FullName}'.");
        }

        var typedFieldConstraints = (IMessage)oneofFieldDescriptor.Accessor.GetValue(fieldConstraints);


        return typedFieldConstraints;
    }

    private class CompiledExpression
    {
        public CompiledExpression(Expression source, CelProgramDelegate celProgramDelegate)
        {
            Source = source;
            CelProgramDelegate = celProgramDelegate;
        }

        public Expression Source { get; }
        public CelProgramDelegate CelProgramDelegate { get; }
    }
}