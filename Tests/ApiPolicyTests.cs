using System.IO;
using System.Security;
using Xunit;

namespace Unbreakable.Tests {
   public class ApiPolicyTests {
        [Fact]
        public void Allows_GenericNewConstraint_IfTypeConstructorIsAllowed() {
            var m = TestHelper.RewriteAndGetMethodWrappedInScope(@"
                using System;
                class C {
                    public void M() {
                        G<DateTime>();
                    }

                    public void G<T>()
                        where T : new()
                    {
                        new T();
                    }
                }
            ", "C", "M");
            // Assert.DoesNotThrow
            m();
        }
    }
}
