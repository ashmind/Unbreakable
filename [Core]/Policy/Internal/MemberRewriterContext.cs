using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unbreakable.Policy.Internal {
    internal struct MemberRewriterContext {
        public MemberRewriterContext(
            ILProcessor il,
            VariableDefinition runtimeGuardVariable,
            RuntimeGuardReferences runtimeGuardReferences
        ) {
            IL = il;
            RuntimeGuardReferences = runtimeGuardReferences;
            RuntimeGuardVariable = runtimeGuardVariable;
        }

        public ILProcessor IL { get; }
        public ModuleDefinition Module => IL.Body.Method.Module;
        public RuntimeGuardReferences RuntimeGuardReferences { get; }
        public VariableDefinition RuntimeGuardVariable { get; }
    }
}
