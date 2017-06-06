using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Unbreakable.Rules {
    public class NamespaceApiRule {
        private readonly IDictionary<string, TypeApiRule> _types = new Dictionary<string, TypeApiRule>();

        internal NamespaceApiRule(ApiAccess access = ApiAccess.Neutral) {
            Access = access;
        }

        [NotNull]
        public NamespaceApiRule Type([NotNull] string typeName, ApiAccess access, [CanBeNull] Action<TypeApiRule> setup = null) {
            Argument.NotNullOrEmpty(nameof(typeName), typeName);
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Type access ({access}) cannot exceed namespace access ({Access}).");

            if (!_types.TryGetValue(typeName, out var rule)) {
                rule = new TypeApiRule();
                _types.Add(typeName, rule);
            }
            rule.Access = access;
            setup?.Invoke(rule);
            return this;
        }

        public ApiAccess Access { get; internal set; }
        public IReadOnlyDictionary<string, TypeApiRule> Types => (IReadOnlyDictionary<string, TypeApiRule>)_types;
    }
}
