using System;
using System.Collections.Generic;

namespace Unbreakable.Rules {
    public class TypeApiRule {
        private readonly IDictionary<string, ApiAccess> _members = new Dictionary<string, ApiAccess>();

        internal TypeApiRule(ApiAccess access = ApiAccess.Neutral) {
            Access = access;
        }

        public ApiAccess Access { get; internal set; }
        public IReadOnlyDictionary<string, ApiAccess> Members => (IReadOnlyDictionary<string, ApiAccess>)_members;

        public void Member(string name, ApiAccess access) {
            Argument.NotNullOrEmpty(nameof(name), name);
            if (access == ApiAccess.Neutral)
                throw new ArgumentOutOfRangeException(nameof(access), "Neutral access is not allowed at member level.");
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Member access ({access}) cannot exceed type access ({Access}).");

            _members[name] = access;
        }
    }
}
