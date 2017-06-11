using System;
using System.Diagnostics;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class TimeGuardTests {
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
            AssertThrowsTimeGuard(code);
        }

        [Theory]
        [InlineData("Enumerable.Range(0, 10000000).Last()")]
        [InlineData("Enumerable.Range(0, 10000000).FirstOrDefault(i => false)")]
        public void ThrowsGuardException_WhenIteratingLargeEnumerable(string expression) {
            // found by Tereza Tomcova (@the_ress)
            AssertThrowsTimeGuard("object M() => " + expression + ";");
        }

        private void AssertThrowsTimeGuard(string code) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System.Linq;
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
