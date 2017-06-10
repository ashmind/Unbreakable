using System;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;

namespace Unbreakable.Tests {
    public class MemoryGuardTests {
        [Theory]
        [InlineData("(new string('a', 50)).Length", 50)]
        [InlineData("(new byte[50]).Length", 50)]
        [InlineData("(new string('a', 25)).Length + (new byte[25]).Length", 50)]
        [InlineData("(new string('a', 25)).Length + (new byte[25]).Length", 50)]
        [InlineData("(new byte[25]).Length + (new byte[25]).Length", 50)]
        public void AllowsAllocations_IfWithinTotalCountLimit(string code, int expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                class C {
                    int M() => " + code + @";
                }
            ", "C", "M");

            Assert.Equal(expected, m());
        }

        [Theory]
        [InlineData("byte[] M() { return new byte[100000]; }", false)]
        [InlineData("byte[] M() { return new byte[(long)int.MaxValue + 1]; }", true)]
        public void ThrowsGuardException_WhenAllocatingLargeArray(string code, bool requiresX64) {
            if (requiresX64)
                Assert.True(IntPtr.Size == 8, "This test can only be run in x64.");

            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                class C {
                    " + code + @"
                }
            ", "C", "M");
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<MemoryGuardException>(exception.InnerException);
        }

        [Theory]
        [InlineData("string M() { return new string('a', 10000); }")]
        public void ThrowsGuardException_WhenAllocatingLargeString(string code) {
            // found by Julien Roncaglia (@vbfox)
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                class C {
                    " + code + @"
                }
            ", "C", "M");
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<MemoryGuardException>(exception.InnerException);
        }
    }
}
