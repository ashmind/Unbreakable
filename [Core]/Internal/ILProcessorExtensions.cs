using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Unbreakable.Internal {
    internal static class ILProcessorExtensions {
        public static Instruction CreateBestLdloc(this ILProcessor il, VariableDefinition variable) {
            switch (variable.Index) {
                case 0: return il.Create(OpCodes.Ldloc_0);
                case 1: return il.Create(OpCodes.Ldloc_1);
                case 2: return il.Create(OpCodes.Ldloc_2);
                case 3: return il.Create(OpCodes.Ldloc_3);
                default: return il.Create(OpCodes.Ldloc, variable);
            }
        }

        public static void InsertBeforeAndRetargetJumps(this ILProcessor il, Instruction target, Instruction instruction) {
            il.InsertBefore(target, instruction);
            foreach (var other in il.Body.Instructions) {
                if (other.Operand == target)
                    other.Operand = instruction;
            }

            if (!il.Body.HasExceptionHandlers)
                return;

            foreach (var handler in il.Body.ExceptionHandlers) {
                if (handler.TryEnd == target)
                    handler.TryEnd = instruction;
                if (handler.HandlerEnd == target)
                    handler.HandlerEnd = instruction;
            }
        }
    }
}
