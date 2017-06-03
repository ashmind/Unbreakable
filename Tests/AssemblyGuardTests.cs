using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Unbreakable.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class AssemblyGuardTests {
        private delegate object Invoke(params object[] args);
        private readonly ITestOutputHelper _output;

        public AssemblyGuardTests(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Rewrite_PreservesSimpleLogic() {
            var m = GetWrappedMethodAfterRewrite(@"
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
        public void Rewrite_PreventsStackOverflow(string code) {
            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<StackLimitException>(exception.InnerException);
        }

        [Theory]
        [InlineData("void M() { while(true) {} }")]
        [InlineData("void M() { again: try { while(true) {} } catch { goto again; } }")]
        public void Rewrite_EnforcesTimeLimit(string code) {
            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            var watch = Stopwatch.StartNew();
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            _output.WriteLine("Time: {0:F2}ms", (double)watch.ElapsedTicks / TimeSpan.TicksPerMillisecond);
            Assert.IsType<TimeLimitException>(exception.InnerException);
        }

        private static Invoke GetWrappedMethodAfterRewrite(string code) {
            var assemblySourceStream = Compile(code);
            var assemblyTargetStream = new MemoryStream();

            assemblySourceStream.Seek(0, SeekOrigin.Begin);
            var token = AssemblyGuard.Rewrite(assemblySourceStream, assemblyTargetStream);

            return args => {
                using (token.Scope()) {
                    var method = GetStandardMethod(assemblyTargetStream);
                    return method(args);
                }
            };
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
