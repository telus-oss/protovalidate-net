using System.Collections;
using System.Collections.Concurrent;
using Buf.Validate.Priv;
using Cel;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ProtoValidate.Exceptions;
using ProtoValidate.Internal.Cel;
using FieldConstraints = Buf.Validate.FieldConstraints;

namespace ProtoValidate.Internal.Constraints;

public class ConstraintCache
{
    /// <summary>
    ///     Map for caching descriptor and their expression delegates
    /// </summary>
    private ConcurrentDictionary<FieldDescriptor, List<CompiledProgram>> DescriptorMap { get; } = new();

    private CelEnvironment CelEnvironment { get; }

    public ConstraintCache(CelEnvironment celEnvironment)
    {
        if (celEnvironment == null)
        {
            throw new ArgumentNullException(nameof(celEnvironment));
        }

        CelEnvironment = celEnvironment;
    }

    public List<CompiledProgram> Compile(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems)
    {
        var compiledProgramList = new List<CompiledProgram>();

        var rulesMessage = ResolveConstraints(fieldDescriptor, fieldConstraints, forItems);
        if (rulesMessage == null)
        {
            // Message null means there were no constraints resolved.
            return new List<CompiledProgram>();
        }

        // Env finalEnv =
        //     env.extend(
        //         EnvOption.types(message.getDefaultInstanceForType()),
        //         EnvOption.declarations(
        //             Decls.newVar(
        //                 Variable.THIS_NAME, DescriptorMappings.getCELType(fieldDescriptor, forItems)),
        //             Decls.newVar(
        //                 Variable.RULES_NAME,
        //                 Decls.newObjectType(message.getDescriptorForType().getFullName()))));
        //ProgramOption rulesOption = ProgramOption.globals(Variable.newRulesVariable(message));


        foreach (var constraintFieldDescriptor in rulesMessage.Descriptor.Fields.InDeclarationOrder())
        {
            if (!DescriptorMap.ContainsKey(constraintFieldDescriptor))
            {
                var options = constraintFieldDescriptor.GetOptions();

                if (options == null)
                {
                    continue;
                }
                //Console.WriteLine();
                //Console.WriteLine($"Checking rule: {constraintFieldDescriptor.FullName}");

                if (constraintFieldDescriptor.HasPresence)
                {
                    var hasRule = constraintFieldDescriptor.Accessor.HasValue(rulesMessage);
                    if (!hasRule)
                    {
                        //Console.WriteLine($"Skipping rule: {constraintFieldDescriptor.FullName}");

                        continue;
                    }
                }
                else if (constraintFieldDescriptor.IsMap)
                {
                    var value = (IDictionary)constraintFieldDescriptor.Accessor.GetValue(rulesMessage);
                    if (value.Count == 0)
                    {
                        //Console.WriteLine($"Skipping rule: {constraintFieldDescriptor.FullName}");

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
                        //Console.WriteLine($"Skipping rule: {constraintFieldDescriptor.FullName}");
                        continue;
                    }
                }

                var constraints = options.GetExtension(PrivateExtensions.Field);

                var expressions = Expression.FromPrivConstraints(constraints.Cel).ToList();

                var compiledPrograms = new List<CompiledProgram>();

                foreach (var expression in expressions)
                {
                    //Console.WriteLine("  --  " + expression.ExpressionText);
                    var celExpression = CelEnvironment.Compile(expression.ExpressionText);
                    // var variables = new Dictionary<string, object?>();
                    // var result = celExpression.Invoke(variables);
                    //
                    //
                    // if (result is string resultString)
                    // {
                    //     if (string.IsNullOrEmpty(resultString))
                    //     {
                    //         continue;
                    //     }
                    // }
                    // else if (result is bool resultBool)
                    // {
                    //     if (resultBool)
                    //     {
                    //         continue;
                    //     }
                    // }


                    var compiledProgram = new CompiledProgram(celExpression, rulesMessage, expression);
                    compiledPrograms.Add(compiledProgram);
                }

                DescriptorMap[constraintFieldDescriptor] = compiledPrograms;
            }

            if (DescriptorMap.TryGetValue(constraintFieldDescriptor, out var compiledProgramsForField))
            {
                compiledProgramList.AddRange(compiledProgramsForField);
            }
        }

        return compiledProgramList;


        //         var programs = new List<CompiledProgram>();
        //         foreach (var astExpression in completeProgramList)
        //         {
        // //run some check here to ensure the expected type is returned
        //         }


        //     List<CompiledProgram> programs = new ArrayList<>();
        //     for (AstExpression astExpression : completeProgramList)
        //     {
        //         try
        //         {
        //             Program program = finalEnv.program(astExpression.ast, rulesOption, PARTIAL_EVAL_OPTIONS);
        //             Program.EvalResult evalResult = program.eval(Activation.emptyActivation());
        //             Val value = evalResult.getVal();
        //             if (value != null)
        //             {
        //                 Object val = value.value();
        //                 if (val instanceof Boolean && value.booleanValue()) {
        //         continue;
        //     }
        //     if (val instanceof String && val.equals("")) {
        //         continue;
        //     }
        // }
        // Ast residual = finalEnv.residualAst(astExpression.ast, evalResult.getEvalDetails());
        // programs.add(
        //         new CompiledProgram(finalEnv.program(residual, rulesOption), astExpression.source));
        //   } catch (Exception e) {
        //     programs.add(
        //         new CompiledProgram(
        //             finalEnv.program(astExpression.ast, rulesOption), astExpression.source));
        //   }
        // }
        // return Collections.unmodifiableList(programs);
    }

    private IMessage? ResolveConstraints(FieldDescriptor fieldDescriptor, FieldConstraints fieldConstraints, bool forItems)
    {

        var fieldOneofs = FieldConstraints.Descriptor.Oneofs;
        if (fieldOneofs == null || fieldOneofs.Count == 0)
        {
            // If the oneof field descriptor is null there are no constraints to resolve.
            return null;
        }

        var oneofDescriptor = fieldOneofs[0];


        // Get the expected constraint descriptor based on the provided field descriptor and the flag
        // indicating whether it is for items.
        var expectedConstraintDescriptor = DescriptorMappings.GetExpectedConstraintDescriptor(fieldDescriptor, forItems);
        if (expectedConstraintDescriptor == null)
        {
            return null;
        }

        var oneofFieldDescriptor = oneofDescriptor.Fields.FirstOrDefault(c => c.FullName == expectedConstraintDescriptor.FullName);
        if (oneofFieldDescriptor == null)
        {
            // If the expected constraint does not match the actual oneof constraint, throw a
            // CompilationError.
            throw new CompilationException($"Expected constraint '{expectedConstraintDescriptor.Name}', got '{null}' on field '{fieldDescriptor.Name}'.");
        }

        return (IMessage)oneofFieldDescriptor.Accessor.GetValue(fieldConstraints);
    }
}