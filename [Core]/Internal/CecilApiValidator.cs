using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

using MemberTypes = System.Reflection.MemberTypes;

namespace Unbreakable.Internal {
    internal static class CecilApiValidator {
        public static void ValidateDefinition(ModuleDefinition definition, IApiFilter filter) {
            ValidateCustomAttributes(definition, filter);
        }

        public static void ValidateDefinition(IMemberDefinition definition, IApiFilter filter) {
            ValidateCustomAttributes(definition, filter);
            // TODO: validate attributes on parameters/return values
        }

        private static void ValidateCustomAttributes(ICustomAttributeProvider provider, IApiFilter filter) {
            if (!provider.HasCustomAttributes)
                return;
            foreach (var attribute in provider.CustomAttributes) {
                ValidateMemberReference(attribute.AttributeType, filter);
                // TODO: Validate attribute arguments
            }
        }

        public static void ValidateInstruction(Instruction instruction, IApiFilter filter) {
            if (!(instruction.Operand is MemberReference reference))
                return;

            if (reference is IMemberDefinition)
                return;

            ValidateMemberReference(reference, filter);
        }

        private static void ValidateMemberReference(MemberReference reference, IApiFilter filter) {
            var type = reference.DeclaringType;
            switch (reference) {
                case MethodReference m:
                    EnsureAllowed(filter, m.ReturnType);
                    EnsureAllowed(filter, m.DeclaringType, m.Name, MemberTypes.Method);
                    break;
                case FieldReference f:
                    EnsureAllowed(filter, f.FieldType);
                    EnsureAllowed(filter, f.DeclaringType, f.Name, MemberTypes.Field);
                    break;
                case TypeReference t:
                    EnsureAllowed(filter, t);
                    break;
                default:
                    throw new NotSupportedException("Unexpected member type '" + reference.GetType() + "'.");
            }
        }

        private static void EnsureAllowed(IApiFilter filter, TypeReference type, string memberName = null, MemberTypes memberType = 0) {
            var result = filter.Filter(type.Namespace, type.Name, memberName, memberType);
            switch (result) {
                case ApiFilterResult.DeniedNamespace:
                    throw new AssemblyGuardException($"Namespace {type.Namespace} is not allowed.");
                case ApiFilterResult.DeniedType:
                    throw new AssemblyGuardException($"Type {type.FullName} is not allowed.");
                case ApiFilterResult.DeniedMember:
                    throw new AssemblyGuardException($"{memberType:G} {type.FullName}.{memberName} is not allowed.");
                case ApiFilterResult.Allowed:
                    return;
                default:
                    throw new NotSupportedException($"Unknown filter result {result}.");
            }
        }
    }
}
