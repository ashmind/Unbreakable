using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unbreakable.Internal;
using Unbreakable.Rules;

namespace Unbreakable {
    public class ApiRules {
        private readonly IDictionary<string, NamespaceApiRule> _namespaces = new Dictionary<string, NamespaceApiRule>();

        public static ApiRules SafeDefaults() => SafeDefaultApiRules.Create();

        internal ApiRules() {
        }

        [NotNull]
        public ApiRules Namespace([NotNull] string @namespace, ApiAccess access, [CanBeNull] Action<NamespaceApiRule> setup = null) {
            Argument.NotNullOrEmpty(nameof(@namespace), @namespace);

            if (!_namespaces.TryGetValue(@namespace, out var rule)) {
                rule = new NamespaceApiRule();
                _namespaces.Add(@namespace, rule);
            }
            rule.Access = access;
            setup?.Invoke(rule);
            return this;
        }

        public TypeApiRule CompilerGeneratedDelegate { get; } = SafeDefaultApiRules.CreateForCompilerGeneratedDelegate();
        public IReadOnlyDictionary<string, NamespaceApiRule> Namespaces => (IReadOnlyDictionary<string, NamespaceApiRule>)_namespaces;
    }
}
