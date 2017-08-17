using JetBrains.Annotations;
using Unbreakable.Policy;

namespace Unbreakable {
    public struct ApiFilterResult {
        public ApiFilterResult(ApiFilterResultKind kind, MemberPolicy memberRule = null) {
            Kind = kind;
            MemberRule = memberRule;
        }

        public ApiFilterResultKind Kind { get; }
        [CanBeNull] public MemberPolicy MemberRule { get; }
    }
}