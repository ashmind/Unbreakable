using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class TimeGuardTests {
        private delegate object Invoke(params object[] args);
        private readonly ITestOutputHelper _output;

        public TimeGuardTests(ITestOutputHelper output) {
            _output = output;
        }

        [Theory]
        [InlineData("void M() { while(true) {} }")]
        [InlineData("void M() { again: try { while(true) {} } catch { goto again; } }")]
        // found by Alexandre Mutel (@xoofx)
        [InlineData("void M() { again: try { while(true) {} } catch {} goto again; }")]
        public void ThrowsGuardException_IfMethodRunsOverDuration(string code) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                class C {
                    " + code + @"
                }"
            , "C", "M");
            var watch = Stopwatch.StartNew();
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            _output.WriteLine("Time: {0:F2}ms", (double)watch.ElapsedTicks / TimeSpan.TicksPerMillisecond);
            Assert.IsType<TimeGuardException>(exception.InnerException);
        }
    }
}
