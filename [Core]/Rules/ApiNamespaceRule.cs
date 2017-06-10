using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Unbreakable.Rules {
    public class ApiNamespaceRule {
        private readonly IDictionary<string, ApiTypeRule> _types = new Dictionary<string, ApiTypeRule>();

        internal ApiNamespaceRule(ApiAccess access = ApiAccess.Neutral) {
            Access = access;
        }

        [NotNull]
        public ApiNamespaceRule Type([NotNull] string typeName, ApiAccess access, [CanBeNull] Action<ApiTypeRule> setup = null) {
            Argument.NotNullOrEmpty(nameof(typeName), typeName);
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Type access ({access}) cannot exceed namespace access ({Access}).");

            if (!_types.TryGetValue(typeName, out var rule)) {
                rule = new ApiTypeRule();
                _types.Add(typeName, rule);
            }
            rule.Access = access;
            setup?.Invoke(rule);
            return this;
        }

        public ApiAccess Access { get; internal set; }
        public IReadOnlyDictionary<string, ApiTypeRule> Types => (IReadOnlyDictionary<string, ApiTypeRule>)_types;
    }
}
