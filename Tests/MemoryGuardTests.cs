using System;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;

namespace Unbreakable.Tests {
    public class MemoryGuardTests {
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
    }
}
