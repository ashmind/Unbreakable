using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Rules.Internal;

namespace Unbreakable.Rules.Rewriters {
    public class CountMemoryGuardRewriter : IApiMemberRewriterInternal {
        private readonly string _countParameterName;

        public CountMemoryGuardRewriter(string countParameterName = "count") {
            _countParameterName = countParameterName;
        }

        int IApiMemberRewriterInternal.Rewrite(Instruction instruction, ApiMemberRewriterContext context) {
            var method = ((MethodReference)instruction.Operand).Resolve();
            var countParameter = GetCountParameter(method);
            if (countParameter == null)
                return 0;
            if (countParameter.Index < method.Parameters.Count - 1)
                throw new NotSupportedException($"{nameof(CountMemoryGuardRewriter)} does not support method {method} because count is not the last argument.");

            var il = context.IL;
            il.InsertBeforeAndRetargetJumps(instruction, il.CreateBestLdloc(context.RuntimeGuardVariable));
            il.InsertBefore(instruction, il.Create(OpCodes.Call, context.RuntimeGuardReferences.GuardCountInt32Method));
            return 2;
        }

        // Unnecessary microoptimization (avoids LINQ allocation)
        private ParameterDefinition GetCountParameter(MethodReference method) {
            foreach (var parameter in method.Parameters) {
                if (parameter.Name == _countParameterName)
                    return parameter;
            }
            return null;
        }
    }
}
