using Mono.Cecil.Cil;

namespace Unbreakable.Policy.Internal {
    internal interface IMemberRewriterInternal : IMemberRewriter {
        string GetShortName();
        bool Rewrite(Instruction instruction, MemberRewriterContext context);
    }
}
