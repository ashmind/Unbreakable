using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Unbreakable.Rules {
    public class ApiTypeRule {
        private IDictionary<string, ApiMemberRule> _members;

        internal ApiTypeRule(ApiAccess access = ApiAccess.Neutral) {
            Access = access;
        }

        public ApiAccess Access { get; internal set; }
        [NotNull]
        public IReadOnlyDictionary<string, ApiMemberRule> Members {
            get {
                EnsureMembers();
                return (IReadOnlyDictionary<string, ApiMemberRule>)_members;
            }
        }

        [NotNull]
        public ApiTypeRule Constructor(ApiAccess access, Action<ApiMemberRule> setup) {
            return Member(".ctor", access, setup);
        }

        [NotNull]
        public ApiTypeRule Constructor(ApiAccess access, params IApiMemberRewriter[] rewriters) {
            return Member(".ctor", access, rewriters);
        }

        [NotNull]
        public ApiTypeRule Getter([NotNull] string propertyName, ApiAccess access, Action<ApiMemberRule> setup) {
            return Member("get_" + propertyName, access, setup);
        }

        [NotNull]
        public ApiTypeRule Getter([NotNull] string propertyName, ApiAccess access, params IApiMemberRewriter[] rewriters) {
            return Member("get_" + propertyName, access, rewriters);
        }

        [NotNull]
        public ApiTypeRule Setter([NotNull] string propertyName, ApiAccess access, Action<ApiMemberRule> setup) {
            return Member("set_" + propertyName, access, setup);
        }

        [NotNull]
        public ApiTypeRule Setter([NotNull] string propertyName, ApiAccess access, params IApiMemberRewriter[] rewriters) {
            return Member("set_" + propertyName, access, rewriters);
        }

        [NotNull]
        public ApiTypeRule Member([NotNull] string name, ApiAccess access, Action<ApiMemberRule> setup) {
            return Member(name, access, null, setup);
        }

        [NotNull]
        public ApiTypeRule Member([NotNull] string name, ApiAccess access, params IApiMemberRewriter[] rewriters) {
            return Member(name, access, rewriters, null);
        }

        private ApiTypeRule Member(string name, ApiAccess access, IApiMemberRewriter[] rewriters, Action<ApiMemberRule> setup) {
            Argument.NotNullOrEmpty(nameof(name), name);
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Member access ({access}) cannot exceed type access ({Access}).");

            EnsureMembers();
            if (!_members.TryGetValue(name, out var member)) {
                member = new ApiMemberRule(access);
                _members.Add(name, member);
            }
            else {
                member.Access = access;
            }
            if (rewriters != null) {
                foreach (var rewriter in rewriters) {
                    member.AddRewriter(rewriter);
                }
            }
            setup?.Invoke(member);
            return this;
        }

        [NotNull]
        public ApiTypeRule Other(params Action<ApiTypeRule>[] setups) {
            foreach (var setup in setups) {
                setup(this);
            }
            return this;
        }

        private void EnsureMembers() {
            if (_members == null)
                Interlocked.CompareExchange(ref _members, new Dictionary<string, ApiMemberRule>(), null);
        }
    }
}
