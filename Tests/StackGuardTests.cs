using System;
using System.Diagnostics;
using System.Reflection;
using Pedantic.IO;
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

        [Theory]
        [InlineData("Finally.cs", Skip = "Currently failing in Github Actions, needs investigation")]
        public void DoesNotThrowGuardException_IfNoStackOverflow(string resourceName) {
            var code = EmbeddedResource.ReadAllText(GetType(), "TestCode." + resourceName);
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(code, "C", "M");

            var exception = Record.Exception(() => m());

            if (exception != null) {
                var inner = Assert.IsType<TargetInvocationException>(exception).InnerException;
                Assert.NotNull(inner);
                Assert.IsNotType<StackGuardException>(inner);
            }
        }
    }
}
