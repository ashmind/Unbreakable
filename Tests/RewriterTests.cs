using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unbreakable.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class RewriterTests {
        private delegate object Invoke(params object[] args);
        private readonly ITestOutputHelper _output;

        public RewriterTests(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Rewrite_PreservesSimpleLogic() {
            var m = GetMethodAfterRewrite(@"
                class C {
                    string M(int a) {
                        return ""x"" + a.ToString();
                    }
                }"
            );
            Assert.Equal("x1", m(1));
        }

        [Theory]
        [InlineData("void M() { M(); }")]
        [InlineData("void M() { M2(); } void M2() { M(); }")]
        public void Rewrite_PreventsStackOverflow(string methodsCode) {
            var m = GetMethodAfterRewrite(@"
                class C {
                    " + methodsCode + @"
                }"
            );
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<StackLimitException>(exception.InnerException);
        }

        private static Invoke GetMethodAfterRewrite(string code) {
            var assemblySourceStream = Compile(code);
            var assemblyTargetStream = new MemoryStream();

            assemblySourceStream.Seek(0, SeekOrigin.Begin);
            new Rewriter().Rewrite(assemblySourceStream, assemblyTargetStream);

            return GetStandardMethod(assemblyTargetStream);
        }

        private static SyntaxTree Parse(string text) {
            return CSharpSyntaxTree.ParseText(text.Replace("                ", ""));
        }
        
        private static MemoryStream Compile(string code) {
            var compilation = CSharpCompilation.Create(
                "_",
                new[] { CSharpSyntaxTree.ParseText(code) },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            Assert.True(result.Success, string.Join("\r\n", result.Diagnostics));
            return stream;
        }

        private static Invoke GetStandardMethod(MemoryStream assemblyStream) {
            var assembly = Assembly.Load(assemblyStream.ToArray());
            var type = assembly.GetType("C");
            var instance = Activator.CreateInstance(type);
            var method = type.GetMethod("M", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return args => method.Invoke(instance, args);
        }
    }
}
