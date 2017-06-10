using System;
using System.Collections.Generic;

namespace Unbreakable.Rules {
    public class ApiTypeRule {
        private readonly IDictionary<string, ApiMemberRule> _members = new Dictionary<string, ApiMemberRule>();

        internal ApiTypeRule(ApiAccess access = ApiAccess.Neutral) {
            Access = access;
        }

        public ApiAccess Access { get; internal set; }
        public IReadOnlyDictionary<string, ApiMemberRule> Members => (IReadOnlyDictionary<string, ApiMemberRule>)_members;

        public ApiTypeRule Constructor(ApiAccess access) {
            return Member(".ctor", access);
        }

        public ApiTypeRule Constructor(ApiAccess access, IApiMemberRewriter rewriter) {
            return Member(".ctor", access, rewriter);
        }

        public ApiTypeRule Member(string name, ApiAccess access) {
            return Member(name, access, null, false);
        }

        public ApiTypeRule Member(string name, ApiAccess access, IApiMemberRewriter rewriter) {
            return Member(name, access, rewriter, true);
        }

        public ApiTypeRule Member(string name, ApiAccess access, IApiMemberRewriter rewriter, bool shouldSetRewriter) {
            Argument.NotNullOrEmpty(nameof(name), name);
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Member access ({access}) cannot exceed type access ({Access}).");

            if (!_members.TryGetValue(name, out var member)) {
                member = new ApiMemberRule(access);
                _members.Add(name, member);
            }
            else {
                member.Access = access;
            }
            if (shouldSetRewriter)
                member.Rewriter = rewriter;
            return this;
        }
    }
}
