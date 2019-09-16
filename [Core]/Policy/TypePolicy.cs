using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Unbreakable.Policy {
    public class TypePolicy {
        private IDictionary<string, MemberPolicy>? _members;

        internal TypePolicy(ApiAccess access = ApiAccess.Neutral) {
            Access = access;
        }

        public ApiAccess Access { get; internal set; }

        public IReadOnlyDictionary<string, MemberPolicy> Members {
            get {
                EnsureMembers();
                return (IReadOnlyDictionary<string, MemberPolicy>)_members!;
            }
        }

        public TypePolicy Constructor(ApiAccess access, Action<MemberPolicy> setup) {
            return Member(".ctor", access, setup);
        }

        public TypePolicy Constructor(ApiAccess access, params IMemberRewriter[] rewriters) {
            return Member(".ctor", access, rewriters);
        }

        public TypePolicy Getter(string propertyName, ApiAccess access, Action<MemberPolicy> setup) {
            return Member("get_" + propertyName, access, setup);
        }

        public TypePolicy Getter(string propertyName, ApiAccess access, params IMemberRewriter[] rewriters) {
            return Member("get_" + propertyName, access, rewriters);
        }

        public TypePolicy Setter(string propertyName, ApiAccess access, Action<MemberPolicy> setup) {
            return Member("set_" + propertyName, access, setup);
        }

        public TypePolicy Setter(string propertyName, ApiAccess access, params IMemberRewriter[] rewriters) {
            return Member("set_" + propertyName, access, rewriters);
        }

        public TypePolicy Member(string name, ApiAccess access, Action<MemberPolicy> setup) {
            return Member(name, access, null, setup);
        }

        public TypePolicy Member(string name, ApiAccess access, params IMemberRewriter[] rewriters) {
            return Member(name, access, rewriters, null);
        }

        private TypePolicy Member(string name, ApiAccess access, IMemberRewriter[]? rewriters, Action<MemberPolicy>? setup) {
            Argument.NotNullOrEmpty(nameof(name), name);
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Member access ({access}) cannot exceed type access ({Access}).");

            EnsureMembers();
            if (!_members!.TryGetValue(name, out var member)) {
                member = new MemberPolicy(access);
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

        public TypePolicy Other(params Action<TypePolicy>[] setups) {
            foreach (var setup in setups) {
                setup(this);
            }
            return this;
        }

        private void EnsureMembers() {
            if (_members == null)
                Interlocked.CompareExchange(ref _members, new Dictionary<string, MemberPolicy>(), null);
        }
    }
}
