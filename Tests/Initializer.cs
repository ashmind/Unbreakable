using System.Runtime.CompilerServices;
using Unbreakable.Policy;

namespace Unbreakable.Tests {
    public static class Initializer {
        [ModuleInitializer]
        public static void Initialize() {
            MemberPolicy.MustVerifyRewritersAllocations = true;
        }
    }
}
