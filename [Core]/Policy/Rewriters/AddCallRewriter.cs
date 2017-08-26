using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class AddCallRewriter : IMemberRewriterInternal {
        public static AddCallRewriter Default { get; } = new AddCallRewriter();

        string IMemberRewriterInternal.GetShortName() => nameof(AddCallRewriter);

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var il = context.IL;

            var ldloc = il.CreateLdlocBest(context.RuntimeGuardVariable);
            var ldlc1 = il.Create(OpCodes.Ldc_I4_1);

            il.InsertAfter(instruction, ldloc);
            il.InsertAfter(ldloc, ldlc1);
            il.InsertAfter(ldlc1, il.CreateCall(context.RuntimeGuardReferences.GuardCountMethod));
            return true;
        }
    }
}
