using System.Linq;
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

        [Fact]
        public void Allows_ReasonableFluentCall() {
            var code = @"
                X.M().M().M().M().M().M().M()
            ";

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
            for (var i = 0; i < 500; i++) {
                builder.Append(prefix).Append(i).Append(open);
            }
            for (var i = 0; i < 500; i++) {
                builder.Append(close);
            }

            Assert.Throws<RoslynGuardException>(
                () => CSharpRoslynGuard.Validate(builder.ToString())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForLongFluentCall() {
            // https://github.com/dotnet/roslyn/issues/9795
            var code = string.Join(".", Enumerable.Repeat("M()", 1000));
            Assert.Throws<RoslynGuardException>(
                () => CSharpRoslynGuard.Validate(code)
            );
        }
    }
}
