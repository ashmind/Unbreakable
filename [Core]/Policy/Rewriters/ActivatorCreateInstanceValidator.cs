using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class ActivatorCreateInstanceValidator : IMemberRewriterInternal {
        public static ActivatorCreateInstanceValidator Default { get; } = new ActivatorCreateInstanceValidator();

        string IMemberRewriterInternal.GetShortName() => nameof(ActivatorCreateInstanceValidator);

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var method = context.Member;
            if (method.GenericParameters.Count != 1 || method.HasParameters)
                throw new AssemblyGuardException("Only Activator.CreateInstance<T>() with no arguments is allowed.");

            var createdType = method.GenericParameters[0].Resolve();
            var constructor = FindDefaultConstuctor(createdType);
            if (constructor == null)
                throw new AssemblyGuardException($"Type '{createdType.Name}' referenced by Activator.CreateInstance<{createdType.Name}>() does not have a default public constructor.");

            context.AssemblyValidator.ValidateMemberReferenceAndGetPolicy(constructor);
            return false;
        }

        private MemberReference FindDefaultConstuctor(TypeDefinition type) {
            if (!type.HasMethods)
                return null;
            foreach (var method in type.Methods) {
                if (method.IsPublic && !method.IsStatic && method.IsConstructor && !method.HasParameters)
                    return method;
            }
            return null;
        }
    }
}
