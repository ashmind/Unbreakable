﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class YetNotSortedTests {
        private delegate object Invoke(params object[] args);
        private readonly ITestOutputHelper _output;

        public YetNotSortedTests(ITestOutputHelper output) {
            _output = output;
        }

        [Theory]
        [InlineData(@"string M(int a) => ""x"" + a.ToString();", "x1")]
        [InlineData(@"int M(int a) => (new[] { a, 2, 3 })[1];", 2)]
        [InlineData(@"
            delegate int F();
            int M(int a) => ((F)(() => a + 1))();
        ", 2)]
        [InlineData(@"bool M(int a) => DateTime.Now.Ticks > 0;", true)] // crash, found by Valery Sarkisov‏ (@VSarkisov)
        public void PreservesStandardLogic(string code, object expected) {
            var m = GetWrappedMethodAfterRewrite(@"
                using System;
                class C {
                    " + code + @"
                }"
            );
            Assert.Equal(expected, m(1));
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
        [InlineData("byte[] M() { return new byte[100000]; }", false)]
        [InlineData("byte[] M() { return new byte[(long)int.MaxValue + 1]; }", true)]
        public void ThrowsMemoryGuardException_WhenAllocatingLargeArray(string code, bool requiresX64) {
            if (requiresX64)
                Assert.True(IntPtr.Size == 8, "This test can only be run in x64.");

            var m = GetWrappedMethodAfterRewrite(@"
                class C {
                    " + code + @"
                }"
            );
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<MemoryGuardException>(exception.InnerException);
        }

        private static Invoke GetWrappedMethodAfterRewrite(string code) {
            var assemblySourceStream = TestHelper.Compile(code);
            var assemblyTargetStream = new MemoryStream();

            var token = AssemblyGuard.Rewrite(assemblySourceStream, assemblyTargetStream);

            return args => {
                using (token.Scope()) {
                    var method = GetStandardMethod(assemblyTargetStream);
                    return method(args);
                }
            };
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
