using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Runtime;

namespace Unbreakable {
    public class AssemblyGuard {
        public static RuntimeGuardToken Rewrite(Stream assemblySourceStream, Stream assemblyTargetStream) {
            var id = Guid.NewGuid();
            var assembly = AssemblyDefinition.ReadAssembly(assemblySourceStream);
            foreach (var module in assembly.Modules) {
                var guardInstanceField = EmitGuardInstance(module, id);
                var guard = new GuardReferences(guardInstanceField, module);

                foreach (var type in module.Types) {
                    foreach (var method in type.Methods) {
                        RewriteMethod(method, guard);
                    }
                }
            }

            assembly.Write(assemblyTargetStream);
            assembly.Write(@"d:\Development\VS 2017\Unbreakable\_.dll");
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

        private static void RewriteMethod(MethodDefinition method, GuardReferences guard) {
            if (!method.HasBody)
                return;

            if (method.DeclaringType == guard.InstanceField.DeclaringType)
                return;

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
                var flowControl = instruction.OpCode.FlowControl;
                if (flowControl == FlowControl.Next || flowControl == FlowControl.Return)
                    continue;

                il.InsertBefore(instruction, il.Create(OpCodes.Ldloc, guardVariable));
                il.InsertBefore(instruction, il.Create(OpCodes.Call, guard.GuardJumpMethod));
                i += 2;
            }
        }

        private struct GuardReferences {
            public GuardReferences(FieldDefinition instanceField, ModuleDefinition module) {
                InstanceField = instanceField;
                GuardEnterMethod = module.Import(typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardEnter)));
                GuardJumpMethod = module.Import(typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardJump)));
            }

            public FieldDefinition InstanceField { get; }
            public MethodReference GuardEnterMethod { get; }
            public MethodReference GuardJumpMethod { get; }
        }
    }
}
