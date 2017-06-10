using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Rules;

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

        public static ApiMemberRule ValidateInstructionAndGetRule(Instruction instruction, IApiFilter filter) {
            if (!(instruction.Operand is MemberReference reference))
                return null;

            return ValidateMemberReference(reference, filter);
        }

        private static ApiMemberRule ValidateMemberReference(MemberReference reference, IApiFilter filter) {
            var type = reference.DeclaringType;
            switch (reference) {
                case MethodReference m: {
                    var memberRule = EnsureAllowed(filter, m.DeclaringType, m.Name);
                    EnsureAllowed(filter, m.ReturnType);
                    return memberRule;
                }
                case FieldReference f: {
                    var memberRule = EnsureAllowed(filter, f.DeclaringType, f.Name);
                    EnsureAllowed(filter, f.FieldType);
                    return memberRule;
                }
                case TypeReference t:
                    EnsureAllowed(filter, t);
                    return null;
                default:
                    throw new NotSupportedException("Unexpected member type '" + reference.GetType() + "'.");
            }
        }

        private static ApiMemberRule EnsureAllowed(IApiFilter filter, TypeReference type, string memberName = null) {
            var typeKind = ApiFilterTypeKind.External;
            if (type is TypeDefinition typeDefinition) {
                if (!IsDelegateDefinition(typeDefinition))
                    return null;
                typeKind = ApiFilterTypeKind.CompilerGeneratedDelegate;
            }
            var result = filter.Filter(type.Namespace, type.Name, typeKind, memberName);
            switch (result.Kind) {
                case ApiFilterResultKind.DeniedNamespace:
                    throw new AssemblyGuardException($"Namespace {type.Namespace} is not allowed.");
                case ApiFilterResultKind.DeniedType:
                    throw new AssemblyGuardException($"Type {type.FullName} is not allowed.");
                case ApiFilterResultKind.DeniedMember:
                    throw new AssemblyGuardException($"Member {type.FullName}.{memberName} is not allowed.");
                case ApiFilterResultKind.Allowed:
                    return result.MemberRule;
                default:
                    throw new NotSupportedException($"Unknown filter result {result}.");
            }
        }

        private static bool IsDelegateDefinition(TypeDefinition type) {
            return type.BaseType != null
                && (
                    type.BaseType.FullName == "System.MulticastDelegate"
                    ||
                    type.BaseType.FullName == "System.Delegate"
                );
        }
    }
}
