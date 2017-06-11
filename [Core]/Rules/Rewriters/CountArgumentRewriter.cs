using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Rules.Internal;

namespace Unbreakable.Rules.Rewriters {
    public class CountArgumentRewriter : IApiMemberRewriterInternal {
        public static CountArgumentRewriter Default { get; } = new CountArgumentRewriter();
        public static CountArgumentRewriter ForCapacity { get; } = new CountArgumentRewriter("capacity");

        private readonly string _countParameterName;

        public CountArgumentRewriter(string countParameterName = "count") {
            _countParameterName = countParameterName;
        }

        bool IApiMemberRewriterInternal.Rewrite(Instruction instruction, ApiMemberRewriterContext context) {
            var method = ((MethodReference)instruction.Operand).Resolve();
            var countParameter = GetCountParameter(method);
            if (countParameter == null)
                return false;
            if (countParameter.Index < method.Parameters.Count - 1)
                throw new NotSupportedException($"{nameof(CountArgumentRewriter)} does not support method {method} because count is not the last argument.");

            var il = context.IL;
            il.InsertBeforeAndRetargetJumps(instruction, il.CreateLdlocBest(context.RuntimeGuardVariable));
            il.InsertBefore(instruction, il.CreateCall(context.RuntimeGuardReferences.GuardCountInt32Method));
            return true;
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
