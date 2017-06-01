using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Runtime;

namespace Unbreakable {
    public class Rewriter {
        public void Rewrite(Stream assemblySourceStream, Stream assemblyTargetStream) {
            var assembly = AssemblyDefinition.ReadAssembly(assemblySourceStream);
            foreach (var module in assembly.Modules) {
                //module.AssemblyReferences.Add(new AssemblyNameReference(runtimeAssemblyName.Name, runtimeAssemblyName.Version));
                var guardType = module.Import(typeof(Guard));
                var guardInstanceField = EmitGuardInstance(guardType, module);
                var guardStackMethod = module.Import(typeof(Guard).GetMethod(nameof(Guard.Stack)));

                foreach (var type in module.Types) {
                    foreach (var method in type.Methods) {
                        RewriteMethod(method, guardInstanceField, guardStackMethod);
                    }
                }
            }

            assembly.Write(assemblyTargetStream);
            assembly.Write(@"d:\Development\VS 2017\Unbreakable\_.dll");
        }

        private static FieldDefinition EmitGuardInstance(TypeReference guardType, ModuleDefinition module) {
            var instanceType = new TypeDefinition(
                "<Unbreakable>", "<GuardInstance>",
                TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NotPublic,
                module.Import(typeof(object))
            );
            var instanceField = new FieldDefinition("Instance", FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly, guardType);
            instanceType.Fields.Add(instanceField);
            
            var constructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static, module.Import(typeof(void)));
            var il = constructor.Body.GetILProcessor();
            il.Emit(OpCodes.Newobj, module.Import(typeof(Guard).GetConstructor(Type.EmptyTypes)));
            il.Emit(OpCodes.Stsfld, instanceField);
            il.Emit(OpCodes.Ret);
            instanceType.Methods.Add(constructor);

            module.Types.Add(instanceType);
            return instanceField;
        }

        private void RewriteMethod(MethodDefinition method, FieldDefinition guardInstanceField, MethodReference guardStackMethod) {
            if (!method.HasBody)
                return;

            if (method.DeclaringType == guardInstanceField.DeclaringType)
                return;

            var il = method.Body.GetILProcessor();
            var start = il.Body.Instructions[0];
            il.InsertBefore(start, il.Create(OpCodes.Ldsfld, guardInstanceField));
            il.InsertBefore(start, il.Create(OpCodes.Call, guardStackMethod));
        }
    }
}
