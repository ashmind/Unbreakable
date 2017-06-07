using System.Text;
using Unbreakable.Roslyn;
using Xunit;

namespace Unbreakable.Tests {
    public class RoslynGuardTests {
        [Fact]
        public void Allows_ReasonableNestingLevel() {
            var code = @"public class C {
                class N {
                    void M() {
                        int MN() {
                           return X.x(() => 5);
                        }
                    }
                }
            }";

            // Assert.DoesNotThrow
            CSharpRoslynGuard.Validate(code);
        }

        [Theory]
        [InlineData("class C", "{", "}")]
        [InlineData("M", "(", ")")]
        [InlineData("a", "[", "]")]
        public void ThrowsGuardException_ForLargeNestingLevel(string prefix, string open, string close) {
            // https://github.com/dotnet/roslyn/issues/20062
            // found by Valery Sarkisov‏(@VSarkisov)
            var builder = new StringBuilder();
            for (var i = 0; i < 100; i++) {
                builder.Append(prefix).Append(i).Append(open);
            }
            for (var i = 0; i < 100; i++) {
                builder.Append(close);
            }

            Assert.Throws<RoslynGuardException>(
                () => CSharpRoslynGuard.Validate(builder.ToString())
            );
        }
    }
}
