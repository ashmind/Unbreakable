using System;
using Mono.Cecil.Cil;
using Unbreakable.Internal;

namespace Unbreakable.Rules.Internal {
    internal struct ApiMemberRewriterContext {
        public ApiMemberRewriterContext(
            ILProcessor il,
            VariableDefinition runtimeGuardVariable,
            RuntimeGuardReferences runtimeGuardReferences
        ) {
            IL = il;
            RuntimeGuardReferences = runtimeGuardReferences;
            RuntimeGuardVariable = runtimeGuardVariable;
        }

        public ILProcessor IL { get; }
        public RuntimeGuardReferences RuntimeGuardReferences { get; }
        public VariableDefinition RuntimeGuardVariable { get; }
    }
}
