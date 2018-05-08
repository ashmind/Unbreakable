using System;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal.AssemblyValidation;
using Unbreakable.Policy;

namespace Unbreakable.Internal {
    internal class AssemblyValidator {
        private readonly AssemblyGuardSettings _settings;
        private readonly AssemblyDefinition _userAssembly;
        private readonly StackSizeValidator _stackSizeValidator;
        private readonly PointerOperationValidator _pointerValidator;

        public AssemblyValidator(
            [NotNull] AssemblyGuardSettings settings,
            [NotNull] AssemblyDefinition userAssembly,
            [NotNull] StackSizeValidator stackSizeValidator,
            [NotNull] PointerOperationValidator pointerValidator
        ) {
            _settings = Argument.NotNull(nameof(settings), settings);
            _userAssembly = Argument.NotNull(nameof(userAssembly), userAssembly);
            _stackSizeValidator = Argument.NotNull(nameof(stackSizeValidator), stackSizeValidator);
            _pointerValidator = Argument.NotNull(nameof(pointerValidator), pointerValidator);
        }

        public void ValidateDefinition([NotNull] ModuleDefinition module) {
            ValidateCustomAttributes(module);
        }

        public void ValidateDefinition([NotNull] TypeDefinition type) {
            // Ensures we don't have to suspect each System.Int32 (etc) to be a user-defined type
            if (type.Namespace == "System" || type.Namespace.StartsWith("System.", StringComparison.Ordinal)) {
                var allowedPattern = _settings.AllowCustomTypesMatchingPatternInSystemNamespaces;
                if (allowedPattern == null || !allowedPattern.IsMatch(type.FullName))
                    throw new AssemblyGuardException($"Custom types cannot be defined in system namespace {type.Namespace}.");
            }

            if ((type.Attributes & TypeAttributes.ExplicitLayout) == TypeAttributes.ExplicitLayout) {
                var allowedPattern = _settings.AllowExplicitLayoutInTypesMatchingPattern;
                if (allowedPattern == null || !allowedPattern.IsMatch(type.FullName))
                    throw new AssemblyGuardException($"Type {type} has an explicit layout which is not allowed.");
            }

            ValidateCustomAttributes(type);
        }

        public void ValidateDefinition([NotNull] MethodDefinition method) {
            if ((method.Attributes & MethodAttributes.PInvokeImpl) == MethodAttributes.PInvokeImpl)
                throw new AssemblyGuardException($"Method {method} uses P/Invoke which is not allowed.");

            foreach (var @override in method.Overrides) {
                if (@override.DeclaringType.FullName == "System.Object" && @override.Name == "Finalize")
                    throw new AssemblyGuardException($"Method {method} is a finalizer which is not allowed.");
            }

            ValidateCustomAttributes(method);
            _stackSizeValidator.ValidateLocals(method);
            // TODO: validate attributes on parameters/return values
        }

        private void ValidateCustomAttributes(ICustomAttributeProvider provider) {
            if (!provider.HasCustomAttributes)
                return;
            foreach (var attribute in provider.CustomAttributes) {
                ValidateMemberReference(attribute.AttributeType);
                // TODO: Validate attribute arguments
            }
        }

        public MemberPolicy ValidateInstructionAndGetPolicy(Instruction instruction, MethodDefinition method) {
            _pointerValidator.ValidateInstruction(instruction, method);
            _stackSizeValidator.ValidateInstruction(instruction, method);

            if (!(instruction.Operand is MemberReference reference))
                return null;

            return ValidateMemberReference(reference);
        }

        private MemberPolicy ValidateMemberReference(MemberReference reference) {
            var type = reference.DeclaringType;
            switch (reference) {
                case MethodReference m: {
                    var memberRule = EnsureAllowed(m.DeclaringType, m.Name);
                    EnsureAllowed(m.ReturnType);
                    return memberRule;
                }
                case FieldReference f: {
                    var memberRule = EnsureAllowed(f.DeclaringType, f.Name);
                    EnsureAllowed(f.FieldType);
                    return memberRule;
                }
                case TypeReference t:
                    EnsureAllowed(t);
                    return null;
                default:
                    throw new NotSupportedException($"Unexpected member type '{reference.GetType()}'.");
            }
        }

        private MemberPolicy EnsureAllowed(TypeReference type, string memberName = null) {
            if (type.IsGenericParameter)
                return null;

            if (type.IsByReference) {
                EnsureAllowed(type.GetElementType());
                if (memberName == null)
                    return null;
                throw new NotSupportedException($"Unsupported reference type member {type.Name}.{memberName}.");
            }

            if ((type is GenericInstanceType generic)) {
                var rule = EnsureAllowed(generic.ElementType, memberName);
                foreach (var argument in generic.GenericArguments) {
                    EnsureAllowed(argument);
                }
                return rule;
            }

            var typeKind = ApiFilterTypeKind.External;
            if (type.IsArray) {
                EnsureAllowed(type.GetElementType());
                if (memberName == null)
                    return null;
                typeKind = ApiFilterTypeKind.Array;
            }
            else if ((type is TypeDefinition typeDefinition) && type.Module.Assembly == _userAssembly) {
                if (!IsDelegateDefinition(typeDefinition))
                    return null;
                typeKind = ApiFilterTypeKind.CompilerGeneratedDelegate;
            }

            var @namespace = GetNamespace(type);
            var typeName = !type.IsNested ? type.Name : (type.FullName.Substring(@namespace.Length + 1).Replace("/", "+"));
            var result = _settings.ApiFilter.Filter(@namespace, typeName, typeKind, memberName);
            switch (result.Kind) {
                case ApiFilterResultKind.DeniedNamespace:
                    throw new AssemblyGuardException($"Namespace {@namespace} is not allowed.");
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

        private string GetNamespace(TypeReference type) {
            string @namespace = null;
            while (string.IsNullOrEmpty(@namespace) && type != null) {
                @namespace = type.Namespace;
                type = type.DeclaringType;
            }
            return @namespace;
        }

        private static bool IsDelegateDefinition(TypeDefinition type) {
            var baseType = type.BaseType;
            return baseType != null
                && (
                    baseType.FullName == "System.MulticastDelegate"
                    ||
                    baseType.FullName == "System.Delegate"
                );
        }
    }
}