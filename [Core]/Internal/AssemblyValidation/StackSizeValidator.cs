using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unbreakable.Internal.AssemblyValidation {
    internal class StackSizeValidator {
        private readonly AssemblyGuardSettings _settings;

        public StackSizeValidator(AssemblyGuardSettings settings) {
            _settings = Argument.NotNull(nameof(settings), settings);
        }

        public void ValidateLocals(MethodDefinition method) {
            if (!method.HasBody)
                return;

            var size = 0;
            foreach (var local in method.Body.Variables) {
                size += TypeSizeCalculator.GetSize(local.VariableType);
            }
            if (size > _settings.MethodLocalsSizeLimit)
                throw new AssemblyGuardException($"Size of locals in method {method} exceeds allowed limit.");
        }

        public void ValidateInstruction(Instruction instruction, MethodDefinition method) {
            if (instruction.OpCode.StackBehaviourPush == StackBehaviour.Push0)
                return;

            var estimatedSize = EstimateSizeOfPush(instruction, method);
            if (estimatedSize > _settings.MethodStackPushSizeLimit)
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
    }
}
