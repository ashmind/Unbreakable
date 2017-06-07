using System;
using System.Diagnostics;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;

namespace Unbreakable.Tests {
    public class StackGuardTests {
        [Theory]
        [InlineData("void M() { M(); }")]
        [InlineData("void M() { M2(); } void M2() { M(); }")]
        public void ThrowsGuardException_InsteadOfStackOverflow(string code) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                class C {
                    " + code + @"
                }
            ", "C", "M");
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<StackGuardException>(exception.InnerException);
        }
    }
}
