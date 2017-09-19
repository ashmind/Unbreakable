using System;
using System.Reflection;
using Unbreakable.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Unbreakable.Tests {
    public class MemoryGuardTests {
        private readonly ITestOutputHelper _output;

        public MemoryGuardTests(ITestOutputHelper output) {
            _output = output;
        }

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

        [Fact]
        public void AllowsAllocations_ByListAdd() {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System.Collections.Generic;
                class C {
                    void M() {
                        var list = new List<int>();
                        list.Add(1);
                    }
                }
            ", "C", "M");

            AssertEx.DoesNotThrow(() => m());
        }

        [Theory]
        [InlineData("new byte[100000]", false)]
        [InlineData("new byte[(long)int.MaxValue + 1]", true)]
        public void ThrowsGuardException_WhenAllocatingLargeArray(string expression, bool requiresX64) {
            if (requiresX64 && IntPtr.Size < 8) {
                _output.WriteLine("This test can only be run in x64.");
                return;
            }
            AssertThrowsMemoryGuard("object M() => " + expression + ";");
        }

        [Theory]
        [InlineData("new string('a', 10000)")]
        public void ThrowsGuardException_WhenAllocatingLargeString(string expression) {
            // found by Julien Roncaglia (@vbfox)
            AssertThrowsMemoryGuard("object M() => " + expression + ";");
        }

        [Theory]
        [InlineData("new List<string>(10000)")]
        [InlineData("new Stack<string>(10000)")]
        [InlineData("new Dictionary<string, string>(10000)")]
        [InlineData("new Queue<string>(10000)")]
        public void ThrowsGuardException_WhenAllocatingLargeCollections(string expression) {
            AssertThrowsMemoryGuard("object M() => " + expression + ";");
        }

        [Theory]
        [InlineData("Enumerable.Range(0, 10000).ToArray()")]
        [InlineData("Enumerable.Range(0, 10000).ToList()")]
        [InlineData("Enumerable.Range(0, 10000).ToDictionary(i => i)")]
        [InlineData("string.Join(\",\", Enumerable.Range(0, 10000))")]
        public void ThrowsGuardException_WhenMaterializingLargeEnumerable(string expression) {
            // found by Tereza Tomcova (@the_ress)
            AssertThrowsMemoryGuard("object M() => " + expression + ";");
        }

        [Theory]
        [InlineData("var list = new List<int>(); for (var i = 0; i < 100000; i++) { list.Add(i); }")]
        public void ThrowsGuardException_WhenAddingToListManyTimes(string code) {
            // found by Tereza Tomcova (@the_ress)
            AssertThrowsMemoryGuard("void M() { " + code + " }");
        }

        [Theory]
        [InlineData("(new[] { 0 }).Intersect(Enumerable.Range(0, 10000)).ToArray()")]
        public void ThrowsGuardException_WhenMatchingToLargeEnumerable(string expression) {
            AssertThrowsMemoryGuard("object M() => " + expression + ";");
        }

        [Theory]
        [InlineData("Enumerable.Range(0, 10000000).Last()")]
        [InlineData("Enumerable.Range(0, 10000000).FirstOrDefault(i => false)")]
        public void ThrowsGuardException_WhenDefiningLargeEnumerable(string expression) {
            // found by Tereza Tomcova (@the_ress)
            AssertThrowsMemoryGuard("object M() => " + expression + ";");
        }

        private static void AssertThrowsMemoryGuard(string code) {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System.Collections.Generic;
                using System.Linq;
                class C {
                    " + code + @"
                }
            ", "C", "M");
            var exception = Assert.Throws<TargetInvocationException>(() => m());
            Assert.IsType<MemoryGuardException>(exception.InnerException);
        }
    }
}
