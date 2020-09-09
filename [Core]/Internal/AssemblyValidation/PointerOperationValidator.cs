using System;
using System.Collections.Generic;
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
                if (!IsSafeZeroOrOneElementPush(valuePush, out bool isPush) || !isPush)
                    return false;

                var pointerPush = FindPreviousInstructionStillOnStackIfSafe(valuePush);
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

        private bool IsSafeZeroOrOneElementPush(Instruction instruction, out bool pushesOneElement) {
            if (IsMethodCall(instruction, out var method)) {
                pushesOneElement = method!.ReturnType.MetadataType != MetadataType.Void;
                return true;
            }

            pushesOneElement = SafeOneElementPushes.Contains(instruction.OpCode.StackBehaviourPush);
            return pushesOneElement;
        }

        private Instruction? FindPreviousInstructionStillOnStackIfSafe(Instruction instruction) {
            var previous = (Instruction?)instruction.Previous;
            if (previous == null)
                return null;

            var skipPushCount = instruction.OpCode.StackBehaviourPop switch {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 => 1,
                StackBehaviour.Popi => 1,
                StackBehaviour.Pop1_pop1 => 2,
                StackBehaviour.Popi_popi => 2,
                StackBehaviour.Varpop => IsMethodCall(instruction, out var method)
                    ? (method!.Parameters.Count + (method.HasThis ? 1 : 0))
                    : (int?)null,
                _ => null
            };

            if (skipPushCount == null)
                return null;

            var pushCount = 0;
            while (pushCount < skipPushCount) {
                if (!IsSafeZeroOrOneElementPush(previous, out var pushed))
                    return null;
                if (pushed)
                    pushCount += 1;
                previous = FindPreviousInstructionStillOnStackIfSafe(previous);
                if (previous == null)
                    return null;
            }
            return previous;
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

        private bool IsMethodCall(Instruction instruction, out MethodReference? method) {
            method = null;
            if (instruction.OpCode.Code != Code.Call && instruction.OpCode.Code != Code.Callvirt)
                return false;

            method = instruction.Operand as MethodReference;
            return method != null;
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
