﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Rules.Internal;

namespace Unbreakable.Rules.Rewriters {
    public class AddCallRewriter : IApiMemberRewriterInternal {
        public static AddCallRewriter Default { get; } = new AddCallRewriter();

        bool IApiMemberRewriterInternal.Rewrite(Instruction instruction, ApiMemberRewriterContext context) {
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
