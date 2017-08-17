using System;
using Mono.Cecil.Cil;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class NoGuardRewriter : IMemberRewriterInternal {
        public static NoGuardRewriter Default { get; } = new NoGuardRewriter();

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            return true;
        }
    }
}
