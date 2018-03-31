using Xunit;

namespace Unbreakable.Tests {
    public class PositiveTests {
        [Theory]
        [InlineData(@"string M(int a) => ""x"" + a.ToString();", "x1")]
        [InlineData(@"int M(int a) => (new[] { a, 2, 3 })[1];", 2)]
        [InlineData(@"
            delegate int F();
            int M(int a) => ((F)(() => a + 1))();
        ", 2)]
        [InlineData(@"bool M(int a) => DateTime.Now.Ticks > 0;", true)] // crash, found by Valery Sarkisov‏ (@VSarkisov)
        [InlineData(@"int M(int _) { MOut(out int x); return x; } void MOut(out int b) { b = 1; }", 1)]
        public void PreservesStandardLogic(string code, object expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                class C {
                    " + code + @"
                }
            ", "C", "M");
            Assert.Equal(expected, m(1));
        }

        [Theory]
        [InlineData("byte M() => System.Text.Encoding.UTF8.GetBytes(\"a\")[0];", (byte)97)]
        [InlineData("string M() => System.Text.Encoding.UTF8.GetString(new byte[] { 97 });", "a")]
        public void HandlesStandardApis(string code, object expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                class C {
                    " + code + @"
                }
            ", "C", "M");
            Assert.Equal(expected, m());
        }
    }
}
