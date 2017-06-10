using JetBrains.Annotations;
using Unbreakable.Rules;

namespace Unbreakable {
    public struct ApiFilterResult {
        public ApiFilterResult(ApiFilterResultKind kind, ApiMemberRule memberRule = null) {
            Kind = kind;
            MemberRule = memberRule;
        }

        public ApiFilterResultKind Kind { get; }
        [CanBeNull] public ApiMemberRule MemberRule { get; }
    }
}