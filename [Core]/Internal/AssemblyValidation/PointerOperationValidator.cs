using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unbreakable.Internal.AssemblyValidation {
    public class PointerOperationValidator {
        private static readonly ISet<StackBehaviour> SafeOneElementPushes = new HashSet<StackBehaviour> {
            StackBehaviour.Push1,
            StackBehaviour.Pushi,
            StackBehaviour.Pushi8,
            StackBehaviour.Pushr4,
            StackBehaviour.Pushr8
        };

        private readonly AssemblyGuardSettings _settings;

        public PointerOperationValidator(AssemblyGuardSettings settings) {
            _settings = Argument.NotNull(nameof(settings), settings);
        }

        public void ValidateInstruction(Instruction instruction, MethodDefinition method) {
            var code = instruction.OpCode.Code;
            if (code == Code.Localloc) {
                // TODO: allow in a limited way, but then it needs to be supported by stack validator
                throw new AssemblyGuardException($"Method {method} performs explicit stack allocation which is not allowed.");
            }

            if (IsDangerousPointerOperation(instruction, method))
                throw new AssemblyGuardException($"Method {method} performs pointer operations which cannot be validated.");
        }

        private bool IsDangerousPointerOperation(Instruction instruction, MethodDefinition method) {
            var code = instruction.OpCode.Code;
            if (code == Code.Ldvirtftn)
                return true;

            var ldType = GetLdType(instruction, method);
            if (ldType != null) {
                if (AllowPointerOperationsInType(method.DeclaringType))
                    return false;
                return ldType.Resolve() != GetElementTypeOfPushedManagedReference(instruction.Previous, method)?.Resolve();
            }

            var stType = GetStType(instruction, method);
            if (stType != null) {
                if (AllowPointerOperationsInType(method.DeclaringType))
                    return false;

                var valuePush = instruction.Previous;
                if (valuePush == null)
                    return true;
                if (!SafeOneElementPushes.Contains(valuePush.OpCode.StackBehaviourPush))
                    return true;

                var pointerPush = FindPreviousInstructionStillOnStack(valuePush);
                return stType.Resolve() != GetElementTypeOfPushedManagedReference(pointerPush, method)?.Resolve();
            }

            return false;
        }

        private TypeReference? GetLdType(Instruction instruction, MethodDefinition method) {
            switch (instruction.OpCode.Code) {
                case Code.Ldind_I: return method.Module.ImportReference(typeof(IntPtr));
                case Code.Ldind_I1: return method.Module.ImportReference(typeof(sbyte));
                case Code.Ldind_I2: return method.Module.ImportReference(typeof(short));
                case Code.Ldind_I4: return method.Module.ImportReference(typeof(int));
                case Code.Ldind_I8: return method.Module.ImportReference(typeof(long));
                case Code.Ldind_R4: return method.Module.ImportReference(typeof(float));
                case Code.Ldind_R8: return method.Module.ImportReference(typeof(double));
                case Code.Ldind_U1: return method.Module.ImportReference(typeof(byte));
                case Code.Ldind_U2: return method.Module.ImportReference(typeof(ushort));
                case Code.Ldind_U4: return method.Module.ImportReference(typeof(uint));
                case Code.Ldind_Ref: return (TypeReference)instruction.Operand;
                case Code.Ldobj: return (TypeReference)instruction.Operand;
                default: return null;
            }
        }

        private TypeReference? GetStType(Instruction instruction, MethodDefinition method) {
            switch (instruction.OpCode.Code) {
                case Code.Stind_I: return method.Module.ImportReference(typeof(IntPtr));
                case Code.Stind_I1: return method.Module.ImportReference(typeof(sbyte));
                case Code.Stind_I2: return method.Module.ImportReference(typeof(short));
                case Code.Stind_I4: return method.Module.ImportReference(typeof(int));
                case Code.Stind_I8: return method.Module.ImportReference(typeof(long));
                case Code.Stind_R4: return method.Module.ImportReference(typeof(float));
                case Code.Stind_R8: return method.Module.ImportReference(typeof(double));
                case Code.Stind_Ref: return (TypeReference)instruction.Operand;
                case Code.Stobj: return (TypeReference)instruction.Operand;
                default: return null;
            }
        }

        private Instruction? FindPreviousInstructionStillOnStack(Instruction instruction) {
            var previous = instruction.Previous;
            if (previous == null)
                return null;

            switch (instruction.OpCode.StackBehaviourPop) {
                case StackBehaviour.Pop0:
                    return previous;
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                    if (!SafeOneElementPushes.Contains(previous.OpCode.StackBehaviourPush))
                        return null;

                    return FindPreviousInstructionStillOnStack(instruction.Previous);
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_popi:
                    if (!SafeOneElementPushes.Contains(previous.OpCode.StackBehaviourPush))
                        return null;

                    var otherPush = FindPreviousInstructionStillOnStack(instruction.Previous);
                    if (!SafeOneElementPushes.Contains(otherPush!.OpCode.StackBehaviourPush))
                        return null;

                    return FindPreviousInstructionStillOnStack(otherPush);
                default:
                    return null;
            }
        }

        private TypeReference? GetElementTypeOfPushedManagedReference(Instruction? instruction, MethodDefinition method) {
            if (instruction == null)
                return null;
            var produced = InferPushedType(instruction, method);
            if (produced == null)
                return null;
            if (produced is RequiredModifierType required)
                produced = required.ElementType;
            if (!produced.IsByReference)
                return null;
            return produced.GetElementType();
        }

        private TypeReference? InferPushedType(Instruction instruction, MethodDefinition method) {
            switch (instruction.OpCode.Code) {
                case Code.Ldarg: return ((ParameterReference)instruction.Operand).ParameterType;
                case Code.Ldarg_0: return GetParameterOrThisType(method, 0);
                case Code.Ldarg_1: return GetParameterOrThisType(method, 1);
                case Code.Ldarg_2: return GetParameterOrThisType(method, 2);
                case Code.Ldarg_3: return GetParameterOrThisType(method, 3);
                case Code.Call: return GetCallReturnType((MethodReference)instruction.Operand);
                default: return null;
            }
        }

        private TypeReference GetParameterOrThisType(MethodDefinition method, int index) {
            if (method.IsStatic)
                return method.Parameters[index].ParameterType;
            if (index == 0)
                return method.DeclaringType;
            return method.Parameters[index - 1].ParameterType;
        }

        private TypeReference GetCallReturnType(MethodReference method) {
            // TODO: Consider generics provided by calling method/type
            return method.ReturnType.ResolveGenericParameters(
                method as GenericInstanceMethod,
                method.DeclaringType as GenericInstanceType
            );
        }

        private bool AllowPointerOperationsInType(TypeDefinition type) {
            return (_settings.AllowPointerOperationsInTypesMatchingPattern?.IsMatch(type.Name) ?? false);
        }
    }
}
