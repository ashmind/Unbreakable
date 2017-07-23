using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unbreakable.Internal {
    internal static class CecilExtensions {
        public static TypeReference ResolveGenericParameters(this TypeReference type, [CanBeNull] GenericInstanceMethod methodInstance, [CanBeNull] GenericInstanceType typeInstance) {
            if (type is GenericParameter genericParameter) {
                switch (genericParameter.Owner) {
                    case MethodReference ownerMethod: {
                        if (ownerMethod.Resolve() != methodInstance?.Resolve())
                            throw new NotSupportedException($"Generic parameter {type} comes from method {ownerMethod} which is different from provided {methodInstance}.");

                        return methodInstance.GenericArguments[genericParameter.Position].ResolveGenericParameters(methodInstance, typeInstance);
                    }

                    case TypeReference ownerType: {
                        if (ownerType.Resolve() != typeInstance?.Resolve())
                            throw new NotSupportedException($"Generic parameter {type} comes from type {ownerType} which is different from provided {typeInstance}.");

                        return typeInstance.GenericArguments[genericParameter.Position].ResolveGenericParameters(methodInstance, typeInstance);
                    }

                    default:
                        throw new NotSupportedException($"Unsupported generic parameter owner: {genericParameter.Owner}.");
                }
            }

            if (type is GenericInstanceType generic) {
                var changed = (GenericInstanceType)null;
                for (var i = 0; i < generic.GenericArguments.Count; i++) {
                    var argument = generic.GenericArguments[i];
                    var resolved = argument.ResolveGenericParameters(methodInstance, typeInstance);
                    if (resolved != argument) {
                        changed = changed ?? Clone(generic);
                        changed.GenericArguments[i] = resolved;
                    }
                }
                return changed ?? type;
            }

            return type;
        }

        private static GenericInstanceType Clone(GenericInstanceType generic) {
            var clone = new GenericInstanceType(generic.ElementType);
            foreach (var argument in generic.GenericArguments) {
                clone.GenericArguments.Add(argument);
            }
            return clone;
        }

        public static Instruction CreateLdlocBest(this ILProcessor il, VariableDefinition variable) {
            switch (variable.Index) {
                case 0: return il.Create(OpCodes.Ldloc_0);
                case 1: return il.Create(OpCodes.Ldloc_1);
                case 2: return il.Create(OpCodes.Ldloc_2);
                case 3: return il.Create(OpCodes.Ldloc_3);
                default: return il.Create(OpCodes.Ldloc, variable);
            }
        }

        public static Instruction CreateStlocBest(this ILProcessor il, VariableDefinition variable) {
            switch (variable.Index) {
                case 0: return il.Create(OpCodes.Stloc_0);
                case 1: return il.Create(OpCodes.Stloc_1);
                case 2: return il.Create(OpCodes.Stloc_2);
                case 3: return il.Create(OpCodes.Stloc_3);
                default: return il.Create(OpCodes.Stloc, variable);
            }
        }

        public static Instruction CreateCall(this ILProcessor il, MethodReference method) {
            return il.Create(OpCodes.Call, method);
        }

        public static void InsertBeforeAndRetargetJumps(this ILProcessor il, Instruction target, Instruction instruction) {
            il.InsertBefore(target, instruction);
            foreach (var other in il.Body.Instructions) {
                if (other.Operand == target)
                    other.Operand = instruction;
            }

            if (!il.Body.HasExceptionHandlers)
                return;

            foreach (var handler in il.Body.ExceptionHandlers) {
                if (handler.TryStart == target)
                    handler.TryStart = instruction;
                if (handler.TryEnd == target)
                    handler.TryEnd = instruction;
                if (handler.FilterStart == target)
                    handler.FilterStart = instruction;
                if (handler.HandlerStart == target)
                    handler.HandlerStart = instruction;
                if (handler.HandlerEnd == target)
                    handler.HandlerEnd = instruction;
            }
        }
    }
}
