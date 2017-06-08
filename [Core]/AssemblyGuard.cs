using System;
using System.IO;
using System.Runtime.InteropServices;
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

                CecilApiValidator.ValidateDefinition(module, settings.ApiFilter);
                foreach (var type in module.Types) {
                    ValidateAndRewriteType(type, guard, settings);
                }
            }

            assembly.Write(assemblyTargetStream);
            //assembly.Write($@"d:\Temp\unbreakable\{Guid.NewGuid()}.dll");
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
            // Ensures we don't have to suspect each System.Int32 (etc) to be a user-defined type
            if (type.Namespace == "System" || type.Namespace.StartsWith("System.", StringComparison.Ordinal))
                throw new AssemblyGuardException($"Custom types cannot be defined in system namespace {type.Namespace}.");

            if ((type.Attributes & TypeAttributes.ExplicitLayout) == TypeAttributes.ExplicitLayout)
                throw new AssemblyGuardException($"Type {type} has an explicit layout which is not allowed.");

            CecilApiValidator.ValidateDefinition(type, settings.ApiFilter);
            foreach (var nested in type.NestedTypes) {
                ValidateAndRewriteType(nested, guard, settings);
            }
            foreach (var method in type.Methods) {
                CecilApiValidator.ValidateDefinition(method, settings.ApiFilter);
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

            ValidateMethodLocalsSize(method, settings);
            if (method.Body.Instructions.Count == 0)
                return; // weird, but happens with 'extern'

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
                CecilApiValidator.ValidateInstruction(instruction, settings.ApiFilter);
                ValidateInstructionStackSize(instruction, method, settings);
                var code = instruction.OpCode.Code;
                if (code == Code.Newarr) {
                    InsertBeforeAndAdjustIfNeeded(il, instruction, il.Create(OpCodes.Ldloc, guardVariable));
                    il.InsertBefore(instruction, il.Create(OpCodes.Call, guard.GuardNewArrayMethod));
                    i += 2;
                    continue;
                }

                var flowControl = instruction.OpCode.FlowControl;
                if (flowControl == FlowControl.Next || flowControl == FlowControl.Return)
                    continue;
                if (instruction.Operand is Instruction target && target.Offset > instruction.Offset)
                    continue;

                InsertBeforeAndAdjustIfNeeded(il, instruction, il.Create(OpCodes.Ldloc, guardVariable));
                il.InsertBefore(instruction, il.Create(OpCodes.Call, guard.GuardJumpMethod));
                i += 2;
            }
        }

        private static void InsertBeforeAndAdjustIfNeeded(ILProcessor il, Instruction original, Instruction before) {
            il.InsertBefore(original, before);
            foreach (var instruction in il.Body.Instructions) {
                if (instruction.Operand == original)
                    instruction.Operand = before;
            }

            if (!il.Body.HasExceptionHandlers)
                return;

            foreach (var handler in il.Body.ExceptionHandlers) {
                if (handler.TryEnd == original)
                    handler.TryEnd = before;
                if (handler.HandlerEnd == original)
                    handler.HandlerEnd = before;
            }
        }

        private static void ValidateMethodLocalsSize(MethodDefinition method, AssemblyGuardSettings settings) {
            var size = 0;
            foreach (var local in method.Body.Variables) {
                size += TypeSizeCalculator.GetSize(local.VariableType);
            }
            if (size > settings.MethodLocalsSizeLimit)
                throw new AssemblyGuardException($"Size of locals in method {method} exceeds allowed limit.");
        }

        private static void ValidateInstructionStackSize(Instruction instruction, MethodDefinition method, AssemblyGuardSettings settings) {
            if (instruction.OpCode.StackBehaviourPush == StackBehaviour.Push0)
                return;

            var estimatedSize = EstimateSizeOfPush(instruction, method);
            if (estimatedSize > settings.MethodStackPushSizeLimit)
                throw new AssemblyGuardException($"Stack push size in method {method} exceeds allowed limit.");
        }

        private static int EstimateSizeOfPush(Instruction instruction, MethodDefinition method) {
            TypeReference GetParameterType(MethodDefinition m, int index) {
                if (!m.IsStatic) {
                    if (index == 0)
                        return m.DeclaringType;
                    index -= 1;
                }
                return m.Parameters[index].ParameterType;
            }
            int GetSize(TypeReference t) => TypeSizeCalculator.GetSize(t);

            switch (instruction.OpCode.Code) {
                case Code.Ldarg_0: return GetSize(GetParameterType(method, 0));
                case Code.Ldarg_1: return GetSize(GetParameterType(method, 1));
                case Code.Ldarg_2: return GetSize(GetParameterType(method, 2));
                case Code.Ldarg_3: return GetSize(GetParameterType(method, 3));
                case Code.Ldloc_0: return GetSize(method.Body.Variables[0].VariableType);
                case Code.Ldloc_1: return GetSize(method.Body.Variables[1].VariableType);
                case Code.Ldloc_2: return GetSize(method.Body.Variables[2].VariableType);
                case Code.Ldloc_3: return GetSize(method.Body.Variables[3].VariableType);
            }

            switch (instruction.Operand) {
                case FieldReference f: return GetSize(f.FieldType);
                case MethodReference m: return GetSize(m.ReturnType);
                case VariableDefinition v: return GetSize(v.VariableType);
                case ParameterDefinition p: return GetSize(p.ParameterType);
                case object o when o != null && o.GetType().IsPrimitive: return Marshal.SizeOf(o);
                default: return sizeof(long); // estimate
            }
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
