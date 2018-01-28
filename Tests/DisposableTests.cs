using System;
using Unbreakable.Policy.Rewriters;
using Xunit;

namespace Unbreakable.Tests {
    public class DisposableTests {
        [Fact]
        public void CollectsDisposable_FromConstructor() {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using Unbreakable.Tests;
                class C {
                    DisposableTests.TestDisposable M() {
                        return new DisposableTests.TestDisposable();
                    }
                }
            ", "C", "M", GetGuardSettingsForTestDisposable());

            var disposable = (TestDisposable)m();

            Assert.True(disposable.Disposed);
        }

        [Fact]
        public void CollectsDisposable_FromMethodCall() {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using Unbreakable.Tests;
                class C {
                    void M(DisposableTests.TestDisposable disposable) {
                        disposable.GetSelfToRewrite();
                    }
                }
            ", "C", "M", GetGuardSettingsForTestDisposable());

            var disposable = new TestDisposable();
            m(disposable);

            Assert.True(disposable.Disposed);
        }

        private AssemblyGuardSettings GetGuardSettingsForTestDisposable() {
            return new AssemblyGuardSettings {
                ApiPolicy = ApiPolicy.SafeDefault().Namespace(
                    "Unbreakable.Tests", ApiAccess.Neutral,
                    n => n.Type(
                        typeof(TestDisposable), ApiAccess.Allowed,
                        t => t.Constructor(ApiAccess.Allowed, new DisposableReturnRewriter())
                              .Member(nameof(TestDisposable.GetSelfToRewrite), ApiAccess.Allowed, new DisposableReturnRewriter())                            
                    )
                )
            };
        }

        public class TestDisposable : IDisposable {
            public IDisposable GetSelfToRewrite() => this;
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }
    }
}
