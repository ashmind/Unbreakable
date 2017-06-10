using System.IO;
using Xunit;

namespace Unbreakable.Tests {
   public class AssemblyGuardTests {
        [Fact]
        public void AllowsExternMethods() {
            // crash, found by Alexandre Mutel‏ (@xoofx)
            var compiled = TestHelper.Compile(@"
                class C
                {
                    static extern void Extern();
                    int M()
                    {
                        Extern();
                        return 0;
                    }
                }
            ");
            // Assert.DoesNotThrow
            AssemblyGuard.Rewrite(compiled, new MemoryStream());
        }

        [Theory]
        [InlineData("void M() { Console.WriteLine('x'); }")]
        [InlineData("void M() { this.GetType(); }")]
        [InlineData("class N { void M() { GC.Collect(); } }")]
        [InlineData("void M() { var x = new IntPtr(0); }")] // crash, found by Alexandre Mutel‏ (@xoofx)
        public void ThrowsGuardException_ForDeniedApi(string code) {
            var compiled = TestHelper.Compile(@"
                using System;
                class C {
                    " + code + @"
                }"
            );
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Theory]
        [InlineData("System")]
        [InlineData("System.Runtime")]
        public void ThrowsGuardException_ForCustomTypesInSystemNamespaces(string @namespace) {
            // found by Llewellyn Pritchard (@leppie)
            var compiled = TestHelper.Compile(@"
                namespace " + @namespace + @" { class C {} }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForDelegateBeginEndInvoke() {
            // found by Llewellyn Pritchard (@leppie)
            var compiled = TestHelper.Compile(@"
                using System;
                class C {
                    delegate void D();
                    void M() { D d = () => {}; d.BeginInvoke(null, null); }
                }"
            );
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForPInvoke() {
            // found by Igal Tabachnik (@hmemcpy) 
            var compiled = TestHelper.Compile(@"
                using System.Runtime.InteropServices;
                static class X {
                    [DllImport(""kernel32.dll"")]
                    static extern void ExitProcess(int uExitCode);
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForFinalizers() {
            // found by George Pollard‏ (@porges)
            var compiled = TestHelper.Compile(@"
                class X {
                    ~X() {}
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForLocalsThatExceedSizeLimit() {
            // found by Stanislav Lukeš‏ (@exyi)
            var compiled = TestHelper.Compile(@"
                using System;
                struct A_1 { public int A; public int B; }
                struct A_2 { public A_1 A; public A_1 B; }
                struct A_3 { public A_2 A; public A_2 B; }
                struct A_4 { public A_3 A; public A_3 B; }
                struct A_5 { public A_4 A; public A_4 B; }
                struct A_6 { public A_5 A; public A_5 B; }
                struct A_7 { public A_6 A; public A_6 B; }
                struct A_8 { public A_7 A; public A_7 B; }
                struct A_9 { public A_8 A; public A_8 B; }
                struct A_10 { public A_9 A; public A_9 B; }
                struct A_11 { public A_10 A; public A_10 B; }
                struct A_12 { public A_11 A; public A_11 B; }
                struct A_13 { public A_12 A; public A_12 B; }
                struct A_14 { public A_13 A; public A_13 B; }
                struct A_15 { public A_14 A; public A_14 B; }
                struct A_16 { public A_15 A; public A_15 B; }
                struct A_17 { public A_16 A; public A_16 B; }
                struct A_18 { public A_17 A; public A_17 B; }
                class C {
                    int M() {
                        A_18 a;
                        return 0;
                    }
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForFieldReadsThatExceedSizeLimit() {
            // found by Tereza Tomcova (@the-ress) 
            var compiled = TestHelper.Compile(@"
                using System;
                using System.Collections.Generic;

                struct A_1 { public int A; public int B; }
                struct A_2 { public A_1 A; public A_1 B; }
                struct A_3 { public A_2 A; public A_2 B; }
                struct A_4 { public A_3 A; public A_3 B; }
                struct A_5 { public A_4 A; public A_4 B; }
                struct A_6 { public A_5 A; public A_5 B; }
                struct A_7 { public A_6 A; public A_6 B; }
                struct A_8 { public A_7 A; public A_7 B; }
                struct A_9 { public A_8 A; public A_8 B; }
                struct A_10 { public A_9 A; public A_9 B; }
                struct A_11 { public A_10 A; public A_10 B; }
                struct A_12 { public A_11 A; public A_11 B; }
                struct A_13 { public A_12 A; public A_12 B; }
                struct A_14 { public A_13 A; public A_13 B; }
                struct A_15 { public A_14 A; public A_14 B; }
                struct A_16 { public A_15 A; public A_15 B; }
                struct A_17 { public A_16 A; public A_16 B; }
                struct A_18 { public A_17 A; public A_17 B; }

                class C {
                    static readonly A_18 Value = default(A_18);

                    int M() {
                        var d = new Dictionary<int, A_18>();
                        d.Add(0, Value);
                        return 0;
                    }
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }

        [Fact]
        public void ThrowsGuardException_ForExplicitStructLayout() {
            // found by Alexandre Mutel (@xoofx) 
            var compiled = TestHelper.Compile(@"
                using System.Runtime.InteropServices;
                [StructLayout(LayoutKind.Explicit)]
                struct S {
                    [FieldOffset(0)]
                    int x;
                }
            ");
            Assert.Throws<AssemblyGuardException>(
                () => AssemblyGuard.Rewrite(compiled, new MemoryStream())
            );
        }
    }
}
