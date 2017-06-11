using System.Linq;
using Mono.Cecil;
using Xunit;
using Unbreakable.Internal;
using Mono.Cecil.Cil;

namespace Unbreakable.Tests.Unit {
    public class CecilExtensionsTests {
        [Fact]
        public void ResolveGenericsParameters_ResolvesTypeToItself_IfNonGeneric() {
            var type = CompileToAssembly("class C {}").MainModule.Types.First(t => t.Name == "C");
            Assert.Same(type, type.ResolveGenericParameters(null));
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

            var resolved = returnType.GetElementType().ResolveGenericParameters(method);

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

            var resolved = returnType.ResolveGenericParameters(method);

            Assert.Equal(
                "System.Int32",
                ((GenericInstanceType)resolved).GenericArguments[0].FullName
            );
        }

        private static AssemblyDefinition CompileToAssembly(string code) {
            return AssemblyDefinition.ReadAssembly(TestHelper.Compile(code));
        }
    }
}
