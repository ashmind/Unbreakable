using System;
using Mono.Cecil.Cil;
using Unbreakable.Rules.Internal;

namespace Unbreakable.Rules.Rewriters {
    public class NoGuardRewriter : IApiMemberRewriterInternal {
        public static NoGuardRewriter Default { get; } = new NoGuardRewriter();

        bool IApiMemberRewriterInternal.Rewrite(Instruction instruction, ApiMemberRewriterContext context) {
            return true;
        }
    }
}
