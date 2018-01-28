using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using ClrTest.Reflection;

namespace Unbreakable.Tools.Analyzer {
    public class Analysis {
        private readonly Dictionary<MethodBase, MethodDetails> _results;

        public Analysis() {
            _results = new Dictionary<MethodBase, MethodDetails>();
        }

        public void RegisterTrusted(MethodBase method) {
            var result = ProcessMethod(method);
            result.IsTrusted = true;
        }

        public void Process(IEnumerable<Assembly> assemblies) {
            foreach (var assembly in assemblies) {
                foreach (var type in assembly.GetExportedTypes()) {
                    if (type.IsAbstract)
                        continue;

                    foreach (var method in type.GetMembers().OfType<MethodBase>()) {
                        if (method.IsAbstract)
                            continue;
                        ProcessMethod(method);
                    }
                }
            }
        }

        private MethodDetails ProcessMethod(MethodBase method) {
            MethodDetails result;
            if (_results.TryGetValue(method, out result))
                return result;
            
            result = new MethodDetails(method);
            _results.Add(method, result); // avoids StackOverflow on recursive calls

            if ((method.MethodImplementationFlags & MethodImplAttributes.InternalCall) == MethodImplAttributes.InternalCall) {
                result.IsInternalCall = true;
                return result;
            }

            foreach (var instruction in new ILReader(method)) {
                ProcessInstruction(instruction, result);
            }
            return result;
        }

        private readonly IReadOnlyDictionary<OpCode, OpCodeDetails> _opCodes = new Dictionary<OpCode, OpCodeDetails> {
            { OpCodes.Add,        new OpCodeDetails {} },
            { OpCodes.Add_Ovf,    new OpCodeDetails {} },
            { OpCodes.Add_Ovf_Un, new OpCodeDetails {} },
            { OpCodes.And,        new OpCodeDetails {} },
            { OpCodes.Beq,        new OpCodeDetails { IsBranch = true } },
            { OpCodes.Beq_S,      new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bge,        new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bge_S,      new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bge_Un,     new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bge_Un_S,   new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bgt,        new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bgt_S,      new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bgt_Un,     new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bgt_Un_S,   new OpCodeDetails { IsBranch = true } },
            { OpCodes.Ble,        new OpCodeDetails { IsBranch = true } },
            { OpCodes.Ble_S,      new OpCodeDetails { IsBranch = true } },
            { OpCodes.Ble_Un,     new OpCodeDetails { IsBranch = true } },
            { OpCodes.Ble_Un_S,   new OpCodeDetails { IsBranch = true } },
            { OpCodes.Blt,        new OpCodeDetails { IsBranch = true } },
            { OpCodes.Blt_S,      new OpCodeDetails { IsBranch = true } },
            { OpCodes.Blt_Un,     new OpCodeDetails { IsBranch = true } },
            { OpCodes.Blt_Un_S,   new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bne_Un,     new OpCodeDetails { IsBranch = true } },
            { OpCodes.Bne_Un_S,   new OpCodeDetails { IsBranch = true } },
            { OpCodes.Brfalse_S,  new OpCodeDetails { IsBranch = true } },
            { OpCodes.Brtrue_S,   new OpCodeDetails { IsBranch = true } },
            { OpCodes.Cgt,        new OpCodeDetails {} },
            { OpCodes.Cgt_Un,     new OpCodeDetails {} },
            { OpCodes.Endfinally, new OpCodeDetails {} },
            { OpCodes.Ldarg_0,    new OpCodeDetails { StackPushArgumentIndex = 0 } },
            { OpCodes.Ldarg_1,    new OpCodeDetails { StackPushArgumentIndex = 1 } },
            { OpCodes.Ldc_I4_0,   new OpCodeDetails { StackPushType = typeof(int) } },
            { OpCodes.Ldc_I4_1,   new OpCodeDetails { StackPushType = typeof(int) } },
            { OpCodes.Ldc_I4_2,   new OpCodeDetails { StackPushType = typeof(int) } },
            { OpCodes.Ldc_I4_8,   new OpCodeDetails { StackPushType = typeof(int) } },
            { OpCodes.Ldc_I4_M1,  new OpCodeDetails { StackPushType = typeof(int) } },
            { OpCodes.Ldc_I4_S,   new OpCodeDetails { StackPushType = typeof(int) } },
            { OpCodes.Ldnull,     new OpCodeDetails { StackPushType = typeof(object) } },
            { OpCodes.Ldfld,      new OpCodeDetails { FieldAccess = new MethodFieldAccess { Read = true }, StackPushOther = StackPushOther.Field } },
            { OpCodes.Ldsfld,     new OpCodeDetails { FieldAccess = new MethodFieldAccess { Read = true }, StackPushOther = StackPushOther.Field } },
            { OpCodes.Leave,      new OpCodeDetails {} },
            { OpCodes.Leave_S,    new OpCodeDetails {} },
            { OpCodes.Call,       new OpCodeDetails { IsCall = true, StackPushOther = StackPushOther.Call } },
            { OpCodes.Callvirt,   new OpCodeDetails { IsCall = true, StackPushOther = StackPushOther.Call } },
            { OpCodes.Newobj,     new OpCodeDetails { IsCall = true, StackPushOther = StackPushOther.NewObj } },
            { OpCodes.Stfld,      new OpCodeDetails { FieldAccess = new MethodFieldAccess { Write = true } } },
            { OpCodes.Pop,        new OpCodeDetails {} },
            { OpCodes.Ret,        new OpCodeDetails {} },
            { OpCodes.Volatile,   new OpCodeDetails {} }
        };

        private void ProcessInstruction(ILInstruction instruction, MethodDetails result) {
            if (!_opCodes.TryGetValue(instruction.OpCode, out OpCodeDetails details)) {
                result.UnknownOpCodes.Add(instruction.OpCode);
                return;
            }

            var method = result.Method;
            if (details.IsBranch)
                ProcessBranchInstruction(instruction, result);

            if (details.IsCall)
                ProcessCallInstruction((InlineMethodInstruction)instruction, result);

            if (details.FieldAccess != null) {
                var field = ((InlineFieldInstruction)instruction).Field;
                result.AddUntrustedFieldAccess(field, details.FieldAccess.Value);
            }

            if (details.StackPushType != null) {
                result.StackPushSize += SizeOf(details.StackPushType);
            }
            else if (details.StackPushArgumentIndex != null) {
                var parameterType = GetParameterType(method, details.StackPushArgumentIndex.Value);
                result.StackPushSize += SizeOf(parameterType);
            }
            else switch (details.StackPushOther) {
                case StackPushOther.Field:
                    var field = ((InlineFieldInstruction)instruction).Field;
                    result.StackPushSize += SizeOf(field.FieldType);
                    break;
                case StackPushOther.Call:
                    var call = (InlineMethodInstruction)instruction;
                    if (call.Method is MethodInfo calledMethod) {
                        var returnType = calledMethod.ReturnType;
                        result.StackPushSize += SizeOf(returnType);
                    }
                    break;
                case StackPushOther.NewObj:
                    var constructedType = ((InlineMethodInstruction)instruction).Method.DeclaringType;
                    result.StackPushSize += SizeOf(constructedType);
                    break;
            }
        }

        private void ProcessBranchInstruction(ILInstruction instruction, MethodDetails result) {
            var target = (instruction is InlineBrTargetInstruction br) ? br.TargetOffset : ((ShortInlineBrTargetInstruction)instruction).TargetOffset;
            if (target < instruction.Offset)
                result.HasLoops = true;
        }

        private void ProcessCallInstruction(ILInstruction instruction, MethodDetails result) {
            var callee = ((InlineMethodInstruction)instruction).Method;
            var calleeDetails = ProcessMethod(callee);
            result.AddCall(calleeDetails);
        }

        private Type GetParameterType(MethodBase method, int index) {
            if (!method.IsStatic) {
                if (index == 0)
                    return method.DeclaringType;
                index -= 1;
            }

            var parameters = method.GetParameters();
            return parameters[index].ParameterType;
        }

        private readonly IDictionary<Type, StackSize> _typeSizeCache = new Dictionary<Type, StackSize>();
        private StackSize SizeOf(Type type) {
            StackSize size;
            if (_typeSizeCache.TryGetValue(type, out size))
                return size;

            size = UncachedSizeOf(type);
            _typeSizeCache.Add(type, size);
            return size;
        }
            
        private StackSize UncachedSizeOf(Type type) {
            if (!type.IsValueType)
                return IntPtr.Size;

            if (type.ContainsGenericParameters)
                return StackSize.DependingOnGenericArguments;

            if (type.IsEnum)
                return SizeOf(type.GetEnumUnderlyingType());

            if (!type.IsPrimitive && (type.IsAutoLayout || type.IsLayoutSequential)) {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var size = (StackSize)0;
                foreach (var field in fields) {
                    size += SizeOf(field.FieldType);
                }
                return size;
            }

            return Marshal.SizeOf(type);
        }

        public IReadOnlyDictionary<MethodBase, MethodDetails> Results => _results;

        private class OpCodeDetails {
            public OpCodeDetails() {
            }
            
            public bool IsCall { get; set; }
            public MethodFieldAccess? FieldAccess { get; set; }

            public bool IsBranch { get; set; }

            public Type StackPushType { get; set; }
            public int? StackPushArgumentIndex { get; set; }
            public StackPushOther? StackPushOther { get; set; }
        }

        private enum StackPushOther {
            Field,
            Call,
            NewObj
        }
    }
}