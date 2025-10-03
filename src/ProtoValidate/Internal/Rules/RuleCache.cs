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
using Cel;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProtoValidate.Exceptions;
using ProtoValidate.Internal.Cel;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ProtoValidate.Internal.Evaluator;
using FieldRules = Buf.Validate.FieldRules;

namespace ProtoValidate.Internal.Rules;

internal class RuleCache
{
    public RuleCache(CelEnvironment celEnvironment, IList<FieldDescriptor> extensions)
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
    internal ConcurrentDictionary<FieldDescriptor, List<CelRule>> DescriptorMap { get; } = new();

    internal CelEnvironment CelEnvironment { get; }
    internal IList<FieldDescriptor> Extensions { get; }

    public List<CompiledProgram> Compile(FieldDescriptor fieldDescriptor, bool forItems, FieldRules fieldRules)
    {
        var compiledProgramList = new List<CompiledProgram>();

        var resolvedRule = ResolveRules(fieldDescriptor, fieldRules, forItems);
        if (resolvedRule == null)
        {
            // Message null means there were no rules resolved.
            return new List<CompiledProgram>();
        }

        //build a cache of all possible rules for this descriptor field
        foreach (var ruleFieldDescriptor in resolvedRule.Rule.Descriptor.Fields.InDeclarationOrder())
        {
            if (!DescriptorMap.ContainsKey(ruleFieldDescriptor))
            {
                var options = ruleFieldDescriptor.GetOptions();

                if (options == null)
                {
                    continue;
                }

                var rules = options.GetExtension(ValidateExtensions.Predefined);

                if (rules == null)
                {
                    rules = new PredefinedRules();
                }

                var compiledPrograms = new List<CelRule>();

                foreach (var rule in rules.Cel)
                {
                    var expression = new Expression(rule);

                    var celExpression = CelEnvironment.Compile(expression.ExpressionText);

                    var rulePath = new FieldPath()
                    {
                        Elements =
                        {
                            resolvedRule.OneofFieldDescriptor.CreateFieldPathElement(),
                            ruleFieldDescriptor.CreateFieldPathElement()
                        }
                    };

                    var compiledExpressionItem = new CelRule(rule, expression, celExpression, ruleFieldDescriptor, forItems, rulePath);
                    compiledPrograms.Add(compiledExpressionItem);
                }

                DescriptorMap[ruleFieldDescriptor] = compiledPrograms;
            }
        }

        //this is where we would compile the extensions, if there are any.
        var ruleType = resolvedRule.Rule.GetType();

        foreach (var extensionFieldDescriptor in Extensions.Where(c => c.IsExtension && c.ExtendeeType.ClrType == ruleType))
        {
            var extensionOptions = extensionFieldDescriptor.GetOptions();
            var predefinedRules = extensionOptions.GetExtension(ValidateExtensions.Predefined);
            if (predefinedRules == null)
            {
                continue;
            }

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

                extensionValue = genericGetExtensionValueMethod.Invoke(resolvedRule.Rule, new object?[] { extension });
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

                var hasExtension = (bool?)genericHasExtensionMethod.Invoke(resolvedRule.Rule, new object?[] { extension }) ?? false;
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

                extensionValue = genericGetExtensionValueMethod.Invoke(resolvedRule.Rule, new object?[] { extension });
            }


            var rulePath = new FieldPath()
            {
                Elements =
                {
                    resolvedRule.OneofFieldDescriptor.CreateFieldPathElement(),
                    extensionFieldDescriptor.CreateFieldPathElement(),
                },
            };
            
            foreach (var rule in predefinedRules.Cel)
            {
                var expression = new Expression(rule);
                var celExpression = CelEnvironment.Compile(expression.ExpressionText);

                var compiledProgram = new CompiledProgram(celExpression, expression, rulePath, null, extensionValue);
                compiledProgramList.Add(compiledProgram);
            }
        }

        //now check to see if we need to add the rule at all based on the specific rules for this field.
        foreach (var ruleFieldDescriptor in resolvedRule.Rule.Descriptor.Fields.InDeclarationOrder())
        {
            if (ruleFieldDescriptor.HasPresence)
            {
                var hasRule = ruleFieldDescriptor.Accessor.HasValue(resolvedRule.Rule);
                if (!hasRule)
                {
                    continue;
                }
            }
            else if (ruleFieldDescriptor.IsMap)
            {
                var value = (IDictionary)ruleFieldDescriptor.Accessor.GetValue(resolvedRule.Rule);
                if (value.Count == 0)
                {
                    continue;
                }
            }
            else if (ruleFieldDescriptor.IsRepeated)
            {
                var value = (IList)ruleFieldDescriptor.Accessor.GetValue(resolvedRule.Rule);
                if (value.Count == 0)
                {
                    continue;
                }
            }
            else if (ruleFieldDescriptor.FieldType == FieldType.Bool)
            {
                var value = (bool)ruleFieldDescriptor.Accessor.GetValue(resolvedRule.Rule);
                if (!value)
                {
                    continue;
                }
            }

            if (DescriptorMap.TryGetValue(ruleFieldDescriptor, out var compiledExpressionList))
            {
                var compiledPrograms = compiledExpressionList.Select(c => new CompiledProgram(c.CelProgramDelegate, c.Source, c.RulePath, resolvedRule.Rule, c.Rule)).ToList();
                compiledProgramList.AddRange(compiledPrograms);
            }
        }

        return compiledProgramList;
    }

    internal ResolvedRule? ResolveRules(FieldDescriptor fieldDescriptor, FieldRules fieldRules, bool forItems)
    {
        var fieldOneofs = FieldRules.Descriptor.Oneofs;
        if (fieldOneofs == null || fieldOneofs.Count == 0)
        {
            // If the oneof field descriptor is null there are no rules to resolve.
            return null;
        }

        var oneofFieldDescriptor = fieldOneofs[0].Accessor.GetCaseFieldDescriptor(fieldRules);
        if (oneofFieldDescriptor == null)
        {
            return null;
        }

        // Get the expected rule descriptor based on the provided field descriptor and the flag
        // indicating whether it is for items.
        var expectedRuleDescriptor = DescriptorMappings.GetExpectedRuleDescriptor(fieldDescriptor, forItems);
        if (expectedRuleDescriptor == null)
        {
            return null;
        }

        if (oneofFieldDescriptor.FullName != expectedRuleDescriptor.FullName)
        {
            // If the expected rule does not match the actual oneof rule, throw a
            // CompilationError.
            throw new CompilationException($"Expected rule '{expectedRuleDescriptor.FullName}', got '{oneofFieldDescriptor.FullName}' on field '{fieldDescriptor.FullName}'.");
        }

        var typedFieldRules = (IMessage)oneofFieldDescriptor.Accessor.GetValue(fieldRules);

        var resolvedRule = new ResolvedRule(typedFieldRules, oneofFieldDescriptor);
        
        return resolvedRule;
    }

    internal class ResolvedRule
    {
        public IMessage Rule { get; }
        public FieldDescriptor OneofFieldDescriptor { get; }

        public ResolvedRule(IMessage rule, FieldDescriptor oneofFieldDescriptor)
        {
            Rule = rule;
            OneofFieldDescriptor = oneofFieldDescriptor;
        }
    }
    internal class CelRule
    {
        public CelRule(Rule rule, Expression source, CelProgramDelegate celProgramDelegate, FieldDescriptor ruleFieldDescriptor, bool forItems, FieldPath rulePath)
        {
            Rule = rule;
            Source = source;
            CelProgramDelegate = celProgramDelegate;
            RuleFieldDescriptor = ruleFieldDescriptor;
            ForItems = forItems;
            RulePath = rulePath;
        }
        public Rule Rule { get; }
        public Expression Source { get; }
        public CelProgramDelegate CelProgramDelegate { get; }
        public FieldDescriptor RuleFieldDescriptor { get; }
        public bool ForItems { get; }
        public FieldPath RulePath { get; }
    }
}