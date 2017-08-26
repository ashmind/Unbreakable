using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Internal;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy.Rewriters {
    public class CollectedEnumerableArgumentRewriter : IMemberRewriterInternal {
        public static CollectedEnumerableArgumentRewriter Default { get; } = new CollectedEnumerableArgumentRewriter();

        string IMemberRewriterInternal.GetShortName() => nameof(CollectedEnumerableArgumentRewriter);

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var method = (MethodReference)instruction.Operand;
            var il = context.IL;

            if (method.Parameters.Count == 1) {
                var enumerableType = method.Parameters[0].ParameterType.ResolveGenericParameters(method as GenericInstanceMethod, method.DeclaringType as GenericInstanceType);
                if (!IsIEnumerable(enumerableType))
                    return false;
                InsertEnumerableGuard(instruction, context, enumerableType);
                return true;
            }

            var parameterTypes = new TypeReference[method.Parameters.Count];
            var hasIEnumerable = false;
            for (var i = 0; i < method.Parameters.Count; i++) {
                var parameterType = method.Parameters[i].ParameterType.ResolveGenericParameters(method as GenericInstanceMethod, method.DeclaringType as GenericInstanceType);
                if (IsIEnumerable(parameterType))
                    hasIEnumerable = true;
                parameterTypes[i] = parameterType;
            }
            if (!hasIEnumerable)
                return false;

            // we need to eat the stack and put it somewhere (vars), so that we can
            // process the enumerables without messing up other values
            var variables = new VariableDefinition[method.Parameters.Count];
            for (var i = method.Parameters.Count - 1; i >= 0; i--) {
                var variable = new VariableDefinition(parameterTypes[i]);
                variables[i] = variable;
                il.Body.Variables.Add(variable);
                il.InsertBeforeAndRetargetJumps(instruction, il.CreateStlocBest(variable));
                // technically we don't have to eat last parameter if it's not enumerable,
                // but trying to keep things simple
            }

            foreach (var variable in variables) {
                il.InsertBefore(instruction, il.CreateLdlocBest(variable));
                if (IsIEnumerable(variable.VariableType))
                    InsertEnumerableGuard(instruction, context, variable.VariableType);
            }

            return true;
        }

        private void InsertEnumerableGuard(Instruction instruction, MemberRewriterContext context, TypeReference enumerableType) {
            var il = context.IL;
            var elementType = ((GenericInstanceType)enumerableType).GenericArguments[0];
            var guardMethodInstance = new GenericInstanceMethod(context.RuntimeGuardReferences.FlowThroughGuardEnumerableCollectedMethod) {
                GenericArguments = { elementType }
            };

            il.InsertBeforeAndRetargetJumps(instruction, il.CreateLdlocBest(context.RuntimeGuardVariable));
            il.InsertBefore(instruction, il.CreateCall(guardMethodInstance));
        }

        private bool IsIEnumerable(TypeReference type) {
            return type.Namespace == "System.Collections.Generic"
                && type.Name == "IEnumerable`1";
        }
    }
}