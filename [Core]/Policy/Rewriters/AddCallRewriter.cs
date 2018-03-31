using System;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class AddCallRewriter : IMemberRewriterInternal {
        public static AddCallRewriter Default { get; } = new AddCallRewriter();

        string IMemberRewriterInternal.GetShortName() => nameof(AddCallRewriter);

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var il = context.IL;
            il.InsertAfter(instruction,
                il.CreateLdlocBest(context.RuntimeGuardVariable),
                il.Create(OpCodes.Ldc_I8, 1L),
                il.CreateCall(context.RuntimeGuardReferences.GuardCountMethod)
            );
            return true;
        }
    }
}
