using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    public static class AssemblyGuard {
        public static RuntimeGuardToken Rewrite(Stream assemblySourceStream, Stream assemblyTargetStream, AssemblyGuardSettings settings = null) {
            Argument.NotNull(nameof(assemblySourceStream), assemblySourceStream);
            Argument.NotNull(nameof(assemblyTargetStream), assemblyTargetStream);
            if (assemblyTargetStream == assemblySourceStream) // Cecil limitation? Causes some weird issues.
                throw new ArgumentException("Target stream must be different from source stream.", nameof(assemblyTargetStream));
            settings = settings ?? AssemblyGuardSettings.Default;

            var id = Guid.NewGuid();
            var assembly = AssemblyDefinition.ReadAssembly(assemblySourceStream);
            foreach (var module in assembly.Modules) {
                var guardInstanceField = EmitGuardInstance(module, id);
                var guard = new GuardReferences(guardInstanceField, module);

                CecilApiValidator.ValidateDefinition(module, settings.Filter);
                foreach (var type in module.Types) {
                    ValidateAndRewriteType(type, guard, settings);
                }
            }

            assembly.Write(assemblyTargetStream);
            return new RuntimeGuardToken(id);
        }

        private static FieldDefinition EmitGuardInstance(ModuleDefinition module, Guid id) {
            var instanceType = new TypeDefinition(
                "<Unbreakable>", "<RuntimeGuardInstance>",
                TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NotPublic,
                module.Import(typeof(object))
            );
            var instanceField = new FieldDefinition(
                "Instance",
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly,
                module.Import(typeof(RuntimeGuard))
            );
            instanceType.Fields.Add(instanceField);
            
            var constructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static, module.Import(typeof(void)));
            var getGuardInstance = module.Import(typeof(RuntimeGuardInstances).GetMethod(nameof(RuntimeGuardInstances.Get)));
            var il = constructor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldstr, id.ToString());
            il.Emit(OpCodes.Call, getGuardInstance);
            il.Emit(OpCodes.Stsfld, instanceField);
            il.Emit(OpCodes.Ret);
            instanceType.Methods.Add(constructor);

            module.Types.Add(instanceType);
            return instanceField;
        }

        private static void ValidateAndRewriteType(TypeDefinition type, GuardReferences guard, AssemblyGuardSettings settings) {
            CecilApiValidator.ValidateDefinition(type, settings.Filter);
            foreach (var nested in type.NestedTypes) {
                ValidateAndRewriteType(nested, guard, settings);
            }
            foreach (var method in type.Methods) {
                CecilApiValidator.ValidateDefinition(method, settings.Filter);
                ValidateAndRewriteMethod(method, guard, settings);
            }
        }

        private static void ValidateAndRewriteMethod(MethodDefinition method, GuardReferences guard, AssemblyGuardSettings settings) {
            if (method.DeclaringType == guard.InstanceField.DeclaringType)
                return;

            if ((method.Attributes & MethodAttributes.PInvokeImpl) == MethodAttributes.PInvokeImpl)
                throw new AssemblyGuardException($"Method {method} uses P/Invoke which is not allowed.");

            foreach (var @override in method.Overrides) {
                if (@override.DeclaringType.FullName == "System.Object" && @override.Name == "Finalize")
                    throw new AssemblyGuardException($"Method {method} is a finalizer which is not allowed.");
            }

            if (!method.HasBody)
                return;

            ValidateMehodLocalsSize(method, settings);

            var il = method.Body.GetILProcessor();
            var guardVariable = new VariableDefinition(guard.InstanceField.FieldType);
            il.Body.Variables.Add(guardVariable);

            var instructions = il.Body.Instructions;
            var start = instructions[0];
            il.InsertBefore(start, il.Create(OpCodes.Ldsfld, guard.InstanceField));
            il.InsertBefore(start, il.Create(OpCodes.Dup));
            il.InsertBefore(start, il.Create(OpCodes.Stloc, guardVariable));
            il.InsertBefore(start, il.Create(OpCodes.Call, guard.GuardEnterMethod));

            for (var i = 4; i < instructions.Count; i++) {
                var instruction = instructions[i];
                CecilApiValidator.ValidateInstruction(instruction, settings.Filter);
                var code = instruction.OpCode.Code;
                if (code == Code.Newarr) {
                    il.InsertBefore(instruction, il.Create(OpCodes.Ldloc, guardVariable));
                    il.InsertBefore(instruction, il.Create(OpCodes.Call, guard.GuardNewArrayMethod));
                    i += 2;
                    continue;
                }

                var flowControl = instruction.OpCode.FlowControl;
                if (flowControl == FlowControl.Next || flowControl == FlowControl.Return)
                    continue;
                if (instruction.Operand is Instruction target && target.Offset > instruction.Offset)
                    continue;

                il.InsertBefore(instruction, il.Create(OpCodes.Ldloc, guardVariable));
                il.InsertBefore(instruction, il.Create(OpCodes.Call, guard.GuardJumpMethod));
                i += 2;
            }
        }

        private static void ValidateMehodLocalsSize(MethodDefinition method, AssemblyGuardSettings settings) {
            var size = 0;
            foreach (var local in method.Body.Variables) {
                size += TypeSizeCalculator.GetSize(local.VariableType);
            }
            if (size > settings.MethodLocalsSizeLimit)
                throw new AssemblyGuardException($"Size of locals in method {method} exceeds allowed limit.");
        }

        private struct GuardReferences {
            public GuardReferences(FieldDefinition instanceField, ModuleDefinition module) {
                InstanceField = instanceField;
                GuardEnterMethod = module.Import(typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardEnter)));
                GuardJumpMethod = module.Import(typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardJump)));
                GuardNewArrayMethod = module.Import(typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardNewArrayFlowThrough)));
            }

            public FieldDefinition InstanceField { get; }
            public MethodReference GuardEnterMethod { get; }
            public MethodReference GuardJumpMethod { get; }
            public MethodReference GuardNewArrayMethod { get; }
        }
    }
}
