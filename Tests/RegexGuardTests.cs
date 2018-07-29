using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unbreakable.Runtime;
using Xunit;

namespace Unbreakable.Tests {
    public class RegexGuardTests {
        [Theory]
        [InlineData("new Regex(\"b\").IsMatch(\"abc\")", true)]
        [InlineData("new Regex(\"b\", RegexOptions.None).IsMatch(\"abc\")", true)]
        [InlineData("new Regex(\"b\").Match(\"abc\").Value", "b")]
        [InlineData("new Regex(\"b\").Replace(\"abc\", \"x\")", "axc")]
        [InlineData("new Regex(\"b\").Replace(\"abc\", \"x\", 1)", "axc")]
        [InlineData("new Regex(\"b\").Replace(\"abc\", \"x\", 1, 0)", "axc")]
        [InlineData("new Regex(\"b\").Replace(\"abc\", _ => \"x\")", "axc")]
        [InlineData("new Regex(\"b\").Replace(\"abc\", _ => \"x\", 1)", "axc")]
        [InlineData("new Regex(\"b\").Replace(\"abc\", _ => \"x\", 1, 0)", "axc")]
        [InlineData("Regex.IsMatch(\"abc\", \"b\")", true)]
        [InlineData("Regex.IsMatch(\"abc\", \"b\", RegexOptions.None)", true)]
        [InlineData("Regex.Match(\"abc\", \"b\").Value", "b")]
        [InlineData("Regex.Match(\"abc\", \"b\", RegexOptions.None).Value", "b")]
        [InlineData("Regex.Replace(\"abc\", \"b\", \"x\")", "axc")]
        [InlineData("Regex.Replace(\"abc\", \"b\", \"x\", RegexOptions.None)", "axc")]
        [InlineData("Regex.Replace(\"abc\", \"b\", _ => \"x\")", "axc")]
        [InlineData("Regex.Replace(\"abc\", \"b\", _ => \"x\", RegexOptions.None)", "axc")]
        public void AllowsMethod(string code, object expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                using System.Text.RegularExpressions;

                class C {
                    public object M() => " + code + @";
                }
            ", "C", "M");
            Assert.Equal(expected, m());
        }

        [Theory]
        [InlineData("new Regex(\"[bd]\").Matches(\"abcd\")", new[] { "b", "d" })]
        [InlineData("new Regex(\"[bd]\", RegexOptions.None).Matches(\"abcd\")", new[] { "b", "d" })]
        [InlineData("Regex.Matches(\"abcd\", \"[bd]\")", new[] { "b", "d" })]
        [InlineData("Regex.Matches(\"abcd\", \"[bd]\", RegexOptions.None)", new[] { "b", "d" })]
        public void AllowsMatches(string code, string[] expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                using System.Text.RegularExpressions;

                class C {
                    public MatchCollection M() => " + code + @";
                }
            ", "C", "M");
            Assert.Equal(
                expected,
                ((MatchCollection)m()).Cast<Match>().Select(x => x.Value).ToArray()
            );
        }

        [Theory]
        [InlineData("new Regex(\"b\").Split(\"abc\")", new[] { "a", "c" })]
        [InlineData("new Regex(\"b\", RegexOptions.None).Split(\"abc\")", new[] { "a", "c" })]
        [InlineData("new Regex(\"b\", RegexOptions.None).Split(\"abc\", 2)", new[] { "a", "c" })]
        [InlineData("new Regex(\"b\", RegexOptions.None).Split(\"abc\", 2, 0)", new[] { "a", "c" })]
        [InlineData("Regex.Split(\"abc\", \"b\")", new[] { "a", "c" })]
        [InlineData("Regex.Split(\"abc\", \"b\", RegexOptions.None)", new[] { "a", "c" })]
        public void AllowsSplit(string code, string[] expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                using System.Text.RegularExpressions;

                class C {
                    public string[] M() => " + code + @";
                }
            ", "C", "M");
            Assert.Equal(expected, (string[])m());
        }

        [Theory]
        [InlineData("new Regex(\"\", RegexOptions.None, TimeSpan.Zero)")]
        [InlineData("Regex.IsMatch(\"\", \"\", RegexOptions.None, TimeSpan.Zero)")]
        [InlineData("Regex.Match(\"\", \"\", RegexOptions.None, TimeSpan.Zero)")]
        [InlineData("Regex.Matches(\"\", \"\", RegexOptions.None, TimeSpan.Zero)")]
        [InlineData("Regex.Split(\"\", \"\", RegexOptions.None, TimeSpan.Zero)")]
        [InlineData("Regex.Replace(\"\", \"\", \"\", RegexOptions.None, TimeSpan.Zero)")]
        [InlineData("Regex.Replace(\"\", \"\", _ => \"\", RegexOptions.None, TimeSpan.Zero)")]
        public void ThrowsAssemblyGuardException_IfTimeoutIsProvidedManually(string code) {
            var compiled = TestHelper.Compile(@"
                using System;
                using System.Text.RegularExpressions;

                class C {
                    public void M() => " + code + @";
                }
            ");
            var ex = Record.Exception(() => AssemblyGuard.Rewrite(compiled, new MemoryStream()));

            Assert.IsType<AssemblyGuardException>(ex);
            Assert.Contains("Timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("new Regex(\"\", (RegexOptions)8)")]
        [InlineData("new Regex(\"\", RegexOptions.Compiled)")]
        [InlineData("new Regex(\"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        [InlineData("Regex.IsMatch(\"\", \"\", (RegexOptions)8)")]
        [InlineData("Regex.IsMatch(\"\", \"\", RegexOptions.Compiled)")]
        [InlineData("Regex.IsMatch(\"\", \"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        [InlineData("Regex.Match(\"\", \"\", (RegexOptions)8)")]
        [InlineData("Regex.Match(\"\", \"\", RegexOptions.Compiled)")]
        [InlineData("Regex.Match(\"\", \"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        [InlineData("Regex.Matches(\"\", \"\", (RegexOptions)8)")]
        [InlineData("Regex.Matches(\"\", \"\", RegexOptions.Compiled)")]
        [InlineData("Regex.Matches(\"\", \"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        [InlineData("Regex.Split(\"\", \"\", (RegexOptions)8)")]
        [InlineData("Regex.Split(\"\", \"\", RegexOptions.Compiled)")]
        [InlineData("Regex.Split(\"\", \"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        [InlineData("Regex.Replace(\"\", \"\", \"\", (RegexOptions)8)")]
        [InlineData("Regex.Replace(\"\", \"\", \"\", RegexOptions.Compiled)")]
        [InlineData("Regex.Replace(\"\", \"\", \"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        [InlineData("Regex.Replace(\"\", \"\", _ => \"\", (RegexOptions)8)")]
        [InlineData("Regex.Replace(\"\", \"\", _ => \"\", RegexOptions.Compiled)")]
        [InlineData("Regex.Replace(\"\", \"\", _ => \"\", RegexOptions.Compiled | RegexOptions.Multiline)")]
        public void ThrowsGuardException_IfUsingRegexOptionsCompiled(string code) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                using System.Text.RegularExpressions;

                class C {
                    public void M() => " + code + @";
                }
            ", "C", "M");

            var ex = Record.Exception(() => m());

            Assert.IsType<GuardException>(Assert.IsType<TargetInvocationException>(ex).InnerException);
        }

        [Theory]
        [InlineData("new Regex(@\"{1}\").Match(\"{0}\")")]
        [InlineData("new Regex(@\"{1}\", RegexOptions.None).Match(\"{0}\")")]
        [InlineData("Regex.IsMatch(\"{0}\", @\"{1}\")")]
        [InlineData("Regex.IsMatch(\"{0}\", @\"{1}\", RegexOptions.None)")]
        [InlineData("Regex.Match(\"{0}\", @\"{1}\")")]
        [InlineData("Regex.Match(\"{0}\", @\"{1}\", RegexOptions.None)")]
        [InlineData("Regex.Split(\"{0}\", @\"{1}\")")]
        [InlineData("Regex.Split(\"{0}\", @\"{1}\", RegexOptions.None)")]
        [InlineData("Regex.Replace(\"{0}\", @\"{1}\", \"\")")]
        [InlineData("Regex.Replace(\"{0}\", @\"{1}\", \"\", RegexOptions.None)")]
        [InlineData("Regex.Replace(\"{0}\", @\"{1}\", _ => \"\")")]
        [InlineData("Regex.Replace(\"{0}\", @\"{1}\", _ => \"\", RegexOptions.None)")]
        // somehow the "evil" regex does not work for Matches
        public void ThrowsTimeoutException_IfRegexTakesTooLong(string code) {
            var input = new string('1', 100) + "x";
            var pattern = @"^(\d+)+$";
            code = string.Format(code, input, pattern);

            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                using System.Text.RegularExpressions;

                class C {
                    public void M() => " + code + @";
                }
            ", "C", "M");

            var ex = Record.Exception(() => m());

            Assert.IsType<RegexMatchTimeoutException>(Assert.IsType<TargetInvocationException>(ex).InnerException);
        }
    }
}
