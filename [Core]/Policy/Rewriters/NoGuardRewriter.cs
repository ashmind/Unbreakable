using Mono.Cecil.Cil;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class NoGuardRewriter : IMemberRewriterInternal {
        public static NoGuardRewriter Default { get; } = new NoGuardRewriter();

        string IMemberRewriterInternal.GetShortName() => nameof(NoGuardRewriter);
        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) => true;
    }
}
