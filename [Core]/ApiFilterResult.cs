using Unbreakable.Policy;

namespace Unbreakable {
    public struct ApiFilterResult {
        public ApiFilterResult(ApiFilterResultKind kind, MemberPolicy? memberRule = null) {
            Kind = kind;
            MemberRule = memberRule;
        }

        public ApiFilterResultKind Kind { get; }
        public MemberPolicy? MemberRule { get; }
    }
}