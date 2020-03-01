using System;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class TimeGuardTests {
        private readonly ITestOutputHelper _output;

        public TimeGuardTests(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Allows_TimeoutEqualTo_TimeSpanMaxValue() {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(
                @"class C { int M() => 1; }", "C", "M",
                runtimeGuardSettings: new RuntimeGuardSettings { TimeLimit = TimeSpan.MaxValue }
            );

            Assert.Equal(1, m());
        }
    }
}
