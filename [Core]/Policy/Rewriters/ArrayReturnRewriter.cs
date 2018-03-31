using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    internal class ArrayReturnRewriter : IMemberRewriterInternal {
        public static ArrayReturnRewriter Default { get; } = new ArrayReturnRewriter();

        public string GetShortName() => nameof(ArrayReturnRewriter);

        public bool Rewrite(Instruction instruction, MemberRewriterContext context) {
            var il = context.IL;

            var method = ((MethodReference)instruction.Operand).Resolve();
            if (!method.ReturnType.IsArray)
                return false;

            var pop = il.Create(OpCodes.Pop);
            context.IL.InsertAfter(instruction,
                il.Create(OpCodes.Dup),
                il.Create(OpCodes.Dup),
                il.Create(OpCodes.Brfalse, pop),
                il.Create(OpCodes.Ldlen),
                il.CreateLdlocBest(context.RuntimeGuardVariable),
                il.CreateCall(context.RuntimeGuardReferences.FlowThroughGuardCountIntPtrMethod),
                pop
            );
            return true;
        }
    }
}
