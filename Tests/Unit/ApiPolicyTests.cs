using Xunit;

namespace Unbreakable.Tests.Unit {
    public class ApiPolicyTests {
        [Fact]
        public void Type_CanAddTypeInEmptyNamespace() {
            var policy = ApiPolicy.SafeDefault();
            policy.Namespace("", ApiAccess.Neutral, n => {
                n.Type(typeof(TestClassWithoutNamespace), ApiAccess.Allowed);
            });

            Assert.Equal(ApiAccess.Allowed, policy.Namespaces[""].Types[nameof(TestClassWithoutNamespace)].Access);
        }
    }
}