using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using Unbreakable.Internal;

using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Unbreakable.Tests.Unit {
    public class CecilExtensionsTests {
        [Fact]
        public void ResolveGenericsParameters_ResolvesTypeToItself_IfNonGeneric() {
            var type = CompileToAssembly("class C {}").MainModule.Types.First(t => t.Name == "C");
            Assert.Same(type, type.ResolveGenericParameters(null, null));
        }

        [Fact]
        public void ResolveGenericsParameters_ResolvesParameters_WhenArgumentIsFromEnclosingMethod() {
            var call = CompileToAssembly(@"
                using System.Linq;
                class C {    
                    void M() => (new int[0]).First();
                }
            ").MainModule
                .Types.First(t => t.Name == "C")
                .Methods.First(m => m.Name == "M")
                .Body.Instructions.First(i => i.OpCode.Code == Code.Call);
            var method = (GenericInstanceMethod)call.Operand;
            var returnType = method.ReturnType;

            var resolved = returnType.GetElementType().ResolveGenericParameters(method, null);

            Assert.Equal("System.Int32", resolved.FullName);
        }

        [Fact]
        public void ResolveGenericsParameters_ResolvesParameterInArguments_WhenArgumentIsFromEnclosingMethod() {
            var call = CompileToAssembly(@"
                using System.Linq;
                class C {    
                    void M() => (new int[0]).Select(i => i);
                }
            ").MainModule
                .Types.First(t => t.Name == "C")
                .Methods.First(m => m.Name == "M")
                .Body.Instructions.First(i => i.OpCode.Code == Code.Call);
            var method = (GenericInstanceMethod)call.Operand;
            var returnType = method.ReturnType;

            var resolved = returnType.ResolveGenericParameters(method, null);

            Assert.Equal(
                "System.Int32",
                ((GenericInstanceType)resolved).GenericArguments[0].FullName
            );
        }

        [Theory]
        [MemberData(nameof(GetShortBranchOpCodes))]
        public void CorrectBranchSizes_CorrectsOpCodeFromShortOne_IfOffsetDoesNotFitSByte(OpCode opCode) {
            var body = new MethodBody(new MethodDefinition("", 0, new TypeReference("", "", null, null)));
            var il = body.GetILProcessor();

            var target = il.Create(OpCodes.Ret);
            il.Emit(opCode, target);
            for (var i = 0; i < 129; i++) {
                il.Emit(OpCodes.Nop);
            }
            il.InsertAfter(body.Instructions.Last(), target);

            il.CorrectAllAfterChanges();

            var corrected = body.Instructions.First();
            Assert.Equal(
                Regex.Replace(opCode.Code.ToString(), "_S$", ""),
                corrected.OpCode.Code.ToString()
            );
        }

        public static IEnumerable<object[]> GetShortBranchOpCodes() {
            var all = typeof(OpCodes)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(OpCode))
                .Select(f => (OpCode)f.GetValue(null));
            return all
                .Where(c => c.OperandType == OperandType.ShortInlineBrTarget)
                .Select(c => new object[] { c });
        }

        private static AssemblyDefinition CompileToAssembly(string code) {
            return AssemblyDefinition.ReadAssembly(TestHelper.Compile(code));
        }
    }
}
