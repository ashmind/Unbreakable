using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class DisposableReturnRewriter : IMemberRewriterInternal {
        public string GetShortName() => "DisposableReturn";

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var il = context.IL;
            var method = ((MethodReference)instruction.Operand).Resolve();
            var collectedType = (method.IsConstructor) ? method.DeclaringType : method.ReturnType;
            var collect = new GenericInstanceMethod(context.RuntimeGuardReferences.FlowThroughCollectDisposableMethod) {
                GenericArguments = { il.Body.Method.Module.Import(collectedType) }
            };
            il.InsertAfter(instruction,
                il.CreateLdlocBest(context.RuntimeGuardVariable),
                il.CreateCall(collect)
            );
            return true;
        }
    }
}
