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
    public class GeneralTests {
        private delegate object Invoke(params object[] args);
        private readonly ITestOutputHelper _output;

        public GeneralTests(ITestOutputHelper output) {
            _output = output;
        }

        [Theory]
        [InlineData(@"string M(int a) => ""x"" + a.ToString();", "x1")]
        [InlineData(@"int M(int a) => (new[] { a, 2, 3 })[1];", 2)]
        [InlineData(@"
            delegate int F();
            int M(int a) => ((F)(() => a + 1))();
        ", 2)]
        public void PreservesStandardLogic(string code, object expected) {
            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            Assert.Equal(expected, m(1));
        }

        [Theory]
        [InlineData("void M() { Console.WriteLine('x'); }")]
        [InlineData("void M() { this.GetType(); }")]
        [InlineData("class N { void M() { GC.Collect(); } }")]
        [InlineData("void M() { var x = new IntPtr(0); }")] // crash, discovered by Alexandre Mutel‏ (@xoofx)
        public void ThrowsAssemblyGuardException_ForDeniedApi(string code) {
            var compiled = Compile(@"
                using System;
                class C {
                    " + code + @"
                }"
            );
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsAssemblyGuardException_ForDelegateBeginEndInvoke() {
            // found by Llewellyn Pritchard (@leppie)
            var compiled = Compile(@"
                using System;
                class C {
                    delegate void D();
                    void M() { D d = () => {}; d.BeginInvoke(null, null); }
                }"
            );
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsAssemblyGuardException_ForPInvoke() {
            // found by Igal Tabachnik (@hmemcpy) 
            var compiled = Compile(@"
                using System.Runtime.InteropServices;
                static class X {
                    [DllImport(""kernel32.dll"")]
                    static extern void ExitProcess(int uExitCode);
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsAssemblyGuardException_ForFinalizers() {
            // found by George Pollard‏ (@porges)
            var compiled = Compile(@"
                class X {
                    ~X() {}
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsAssemblyGuardException_ForLocalsThatExceedSizeLimit() {
            // found by Stanislav Lukeš‏ (@exyi)
            var compiled = Compile(@"
                using System;
                struct A_1 { public int A; public int B; }
                struct A_2 { public A_1 A; public A_1 B; }
                struct A_3 { public A_2 A; public A_2 B; }
                struct A_4 { public A_3 A; public A_3 B; }
                struct A_5 { public A_4 A; public A_4 B; }
                struct A_6 { public A_5 A; public A_5 B; }
                struct A_7 { public A_6 A; public A_6 B; }
                struct A_8 { public A_7 A; public A_7 B; }
                struct A_9 { public A_8 A; public A_8 B; }
                struct A_10 { public A_9 A; public A_9 B; }
                struct A_11 { public A_10 A; public A_10 B; }
                struct A_12 { public A_11 A; public A_11 B; }
                struct A_13 { public A_12 A; public A_12 B; }
                struct A_14 { public A_13 A; public A_13 B; }
                struct A_15 { public A_14 A; public A_14 B; }
                struct A_16 { public A_15 A; public A_15 B; }
                struct A_17 { public A_16 A; public A_16 B; }
                struct A_18 { public A_17 A; public A_17 B; }
                class C {
                    int M() {
                        A_18 a;
                        return 0;
                    }
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Theory]
        [InlineData("void M() { M(); }")]
        [InlineData("void M() { M2(); } void M2() { M(); }")]
        public void ThrowsStackGuardException_InsteadOfStackOverflow(string code) {
            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<StackGuardException>(exception.InnerException);
        }

        [Theory]
        [InlineData("void M() { while(true) {} }")]
        [InlineData("void M() { again: try { while(true) {} } catch { goto again; } }")]
        public void ThrowsTimeGuardException_IfMethodRunsOverDuration(string code) {
            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            var watch = Stopwatch.StartNew();
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            _output.WriteLine("Time: {0:F2}ms", (double)watch.ElapsedTicks / TimeSpan.TicksPerMillisecond);
            Assert.IsType<TimeGuardException>(exception.InnerException);
        }

        [Theory]
        [InlineData("byte[] M() { return new byte[10000000]; }")]
        public void ThrowsMemoryGuardException_WhenAllocatingLargeArray(string code) {
            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<MemoryGuardException>(exception.InnerException);
        }

        private static Invoke GetWrappedMethodAfterRewrite(string code) {
            var assemblySourceStream = Compile(code);
            var assemblyTargetStream = new MemoryStream();
                        
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
            stream.Seek(0, SeekOrigin.Begin);
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
