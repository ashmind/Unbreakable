using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Internal.AssemblyValidation;
using Unbreakable.Policy.Internal;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    public static class AssemblyGuard {
        public static RuntimeGuardToken Rewrite(Stream assemblySourceStream, Stream assemblyTargetStream, AssemblyGuardSettings settings = null) {
            Argument.NotNull(nameof(assemblySourceStream), assemblySourceStream);
            Argument.NotNull(nameof(assemblyTargetStream), assemblyTargetStream);
            if (assemblyTargetStream == assemblySourceStream) // Cecil limitation? Causes some weird issues.
                throw new ArgumentException("Target stream must be different from source stream.", nameof(assemblyTargetStream));

            var assembly = AssemblyDefinition.ReadAssembly(assemblySourceStream);
            var token = Rewrite(assembly, settings);
            assembly.Write(assemblyTargetStream);
            //assembly.Write($@"d:\Temp\unbreakable\{Guid.NewGuid()}.dll");
            return token;
        }

        internal static RuntimeGuardToken Rewrite(AssemblyDefinition assembly, AssemblyGuardSettings settings = null) {
            var id = Guid.NewGuid();
            settings = settings ?? AssemblyGuardSettings.Default;
            var validator = new AssemblyValidator(settings, assembly, new StackSizeValidator(settings), new PointerOperationValidator(settings));
            foreach (var module in assembly.Modules) {
                var guardInstanceField = EmitGuardInstance(module, id);
                var guard = new RuntimeGuardReferences(guardInstanceField, module);

                validator.ValidateDefinition(module);
                foreach (var type in module.Types) {
                    ValidateAndRewriteType(type, guard, validator, settings);
                }
            }
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

        private static void ValidateAndRewriteType(TypeDefinition type, RuntimeGuardReferences guard, AssemblyValidator validator, AssemblyGuardSettings settings) {
            validator.ValidateDefinition(type);
            foreach (var nested in type.NestedTypes) {
                ValidateAndRewriteType(nested, guard, validator, settings);
            }
            foreach (var method in type.Methods) {
                ValidateAndRewriteMethod(method, guard, validator, settings);
            }
        }

        private static void ValidateAndRewriteMethod(MethodDefinition method, RuntimeGuardReferences guard, AssemblyValidator validator, AssemblyGuardSettings settings) {
            if (method.DeclaringType == guard.InstanceField.DeclaringType)
                return;

            validator.ValidateDefinition(method);
            if (!method.HasBody)
                return;
            
            if (method.Body.Instructions.Count == 0)
                return; // weird, but happens with 'extern'

            var isStaticConstructor = method.Name == ".cctor" && method.IsStatic && method.IsRuntimeSpecialName;
            var il = method.Body.GetILProcessor();
            var guardVariable = new VariableDefinition(guard.InstanceField.FieldType);
            il.Body.Variables.Add(guardVariable);

            var instructions = il.Body.Instructions;
            var start = instructions[0];
            var skipFirst = 4;
            il.InsertBefore(start, il.Create(OpCodes.Ldsfld, guard.InstanceField));
            il.InsertBefore(start, il.Create(OpCodes.Dup));
            il.InsertBefore(start, il.CreateStlocBest(guardVariable));
            il.InsertBefore(start, il.Create(OpCodes.Call, isStaticConstructor ? guard.GuardEnterStaticConstructorMethod : guard.GuardEnterMethod));

            for (var i = skipFirst; i < instructions.Count; i++) {
                var instruction = instructions[i];
                var memberRule = validator.ValidateInstructionAndGetPolicy(instruction, method);
                var code = instruction.OpCode.Code;
                if (code == Code.Newarr) {
                    il.InsertBeforeAndRetargetJumps(instruction, il.CreateLdlocBest(guardVariable));
                    il.InsertBefore(instruction, il.CreateCall(guard.FlowThroughGuardCountIntPtrMethod));
                    i += 2;
                    continue;
                }

                if (memberRule != null && memberRule.Rewriters.Count > 0) {
                    var instructionCountBefore = instructions.Count;
                    var rewritten = false;
                    foreach (var rewriter in memberRule.InternalRewriters) {
                        rewritten = rewriter.Rewrite(
                            instruction, new MemberRewriterContext(il, guardVariable, guard)
                        ) || rewritten;
                    }
                    if (rewritten) {
                        i += instructions.Count - instructionCountBefore;
                        continue;
                    }
                }

                if (isStaticConstructor && instruction.OpCode.Code == Code.Ret) {
                    il.InsertBeforeAndRetargetJumps(instruction, il.CreateLdlocBest(guardVariable));
                    il.InsertBefore(instruction, il.CreateCall(guard.GuardExitStaticConstructorMethod));
                    i += 2;
                    continue;
                }

                if (!ShouldInsertJumpGuardBefore(instruction))
                    continue;

                il.InsertBeforeAndRetargetJumps(instruction, il.CreateLdlocBest(guardVariable));
                il.InsertBefore(instruction, il.Create(OpCodes.Call, guard.GuardJumpMethod));
                i += 2;
            }

            il.CorrectAllAfterChanges();
        }

        private static bool ShouldInsertJumpGuardBefore(Instruction instruction, bool ignorePrefix = false) {
            var opCode = instruction.OpCode;
            if (opCode.OpCodeType == OpCodeType.Prefix)
                return ShouldInsertJumpGuardBefore(instruction.Next, ignorePrefix: true);

            if (!ignorePrefix && instruction.Previous?.OpCode.OpCodeType == OpCodeType.Prefix)
                return false;

            var flowControl = opCode.FlowControl;
            if (flowControl == FlowControl.Next || flowControl == FlowControl.Return)
                return false;

            if (instruction.Operand is Instruction target && target.Offset > instruction.Offset)
                return false;

            return true;
        }

        private static bool IsCallToUserCode(Instruction instruction, AssemblyDefinition userCodeAssembly) {
            var code = instruction.OpCode.Code;
            return (code == Code.Call || code == Code.Calli || code == Code.Callvirt)
                && ((MethodReference)instruction.Operand).Module.Assembly == userCodeAssembly;
        }
    }
}
