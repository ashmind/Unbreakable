using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;

namespace Unbreakable.Policy.Internal {
    internal struct MemberRewriterContext {
        public MemberRewriterContext(
            ILProcessor il,
            MethodDefinition member,
            VariableDefinition runtimeGuardVariable,
            RuntimeGuardReferences runtimeGuardReferences,
            AssemblyValidator assemblyValidator
        ) {
            IL = il;
            Member = member;
            RuntimeGuardReferences = runtimeGuardReferences;
            RuntimeGuardVariable = runtimeGuardVariable;
            AssemblyValidator = assemblyValidator;
        }

        public ILProcessor IL { get; }
        public MethodDefinition Member { get; }
        public RuntimeGuardReferences RuntimeGuardReferences { get; }
        public VariableDefinition RuntimeGuardVariable { get; }
        public AssemblyValidator AssemblyValidator { get; }
    }
}
