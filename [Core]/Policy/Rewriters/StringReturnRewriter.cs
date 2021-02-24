using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    internal class StringReturnRewriter : IMemberRewriterInternal {
        public static StringReturnRewriter Default { get; } = new StringReturnRewriter();

        public string GetShortName() => nameof(StringReturnRewriter);

        public bool Rewrite(Instruction instruction, MemberRewriterContext context) {
            var il = context.IL;

            var method = ((MethodReference)instruction.Operand).Resolve();
            if (method.ReturnType.FullName != typeof(string).FullName)
                return false;

            var getLength = il.Body.Method.Module.ImportReference(
                method.ReturnType.Resolve().GetProperty(nameof(string.Length))!.GetMethod
            );
            var pop = il.Create(OpCodes.Pop);
            context.IL.InsertAfter(instruction,
                il.Create(OpCodes.Dup),
                il.Create(OpCodes.Dup),
                il.Create(OpCodes.Brfalse, pop),
                il.CreateCall(getLength),
                il.CreateLdlocBest(context.RuntimeGuardVariable),
                il.CreateCall(context.RuntimeGuardReferences.FlowThroughGuardCountIntPtrMethod),
                pop
            );
            return true;
        }
    }
}
