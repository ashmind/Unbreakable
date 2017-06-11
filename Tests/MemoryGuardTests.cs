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
        [InlineData("(new byte[25]).Length + (new byte[25]).Length", 50)]
        [InlineData("(new System.Collections.Generic.List<string>(50)).Capacity", 50)]
        [InlineData("string.Join(\"\", Enumerable.Repeat('a', 50)).Length", 50)]
        public void AllowsAllocations_IfWithinTotalCountLimit(string code, int expected) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System.Linq;
                class C {
                    int M() => " + code + @";
                }
            ", "C", "M");

            Assert.Equal(expected, m());
        }

        [Theory]
        [InlineData("new byte[100000]", false)]
        [InlineData("new byte[(long)int.MaxValue + 1]", true)]
        public void ThrowsGuardException_WhenAllocatingLargeArray(string expression, bool requiresX64) {
            if (requiresX64)
                Assert.True(IntPtr.Size == 8, "This test can only be run in x64.");
            AssertThrowsMemoryGuard(expression);
        }

        [Theory]
        [InlineData("new string('a', 10000)")]
        public void ThrowsGuardException_WhenAllocatingLargeString(string expression) {
            // found by Julien Roncaglia (@vbfox)
            AssertThrowsMemoryGuard(expression);
        }

        [Theory]
        [InlineData("new List<string>(10000)")]
        [InlineData("new Stack<string>(10000)")]
        [InlineData("new Dictionary<string, string>(10000)")]
        [InlineData("new Queue<string>(10000)")]
        public void ThrowsGuardException_WhenAllocatingLargeCollections(string expression) {
            AssertThrowsMemoryGuard(expression);
        }

        [Theory]
        [InlineData("Enumerable.Range(0, 10000).ToArray()")]
        [InlineData("Enumerable.Range(0, 10000).ToList()")]
        [InlineData("Enumerable.Range(0, 10000).ToDictionary(i => i)")]
        [InlineData("string.Join(\",\", Enumerable.Range(0, 10000))")]
        public void ThrowsGuardException_WhenMaterializingLargeEnumerable(string expression) {
            // found by Tereza Tomcova (@the_ress)
            AssertThrowsMemoryGuard(expression);
        }

        [Theory]
        [InlineData("(new[] { 0 }).Intersect(Enumerable.Range(0, 10000)).ToArray()")]
        public void ThrowsGuardException_WhenMatchingToLargeEnumerable(string expression) {
            AssertThrowsMemoryGuard(expression);
        }

        private static void AssertThrowsMemoryGuard(string expression) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System.Collections.Generic;
                using System.Linq;
                class C {
                    object M() => " + expression + @";
                }
            ", "C", "M");
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<MemoryGuardException>(exception.InnerException);
        }
    }
}
