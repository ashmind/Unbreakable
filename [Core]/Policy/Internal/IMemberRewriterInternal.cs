using Mono.Cecil.Cil;

namespace Unbreakable.Policy.Internal {
    internal interface IMemberRewriterInternal : IMemberRewriter {
        bool Rewrite(Instruction instruction, MemberRewriterContext context);
    }
}
