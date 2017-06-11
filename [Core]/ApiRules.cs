using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unbreakable.Internal;
using Unbreakable.Rules;

namespace Unbreakable {
    public class ApiRules {
        private readonly IDictionary<string, ApiNamespaceRule> _namespaces = new Dictionary<string, ApiNamespaceRule>();

        public static ApiRules SafeDefaults() => SafeDefaultApiRules.Create();

        internal ApiRules(ApiTypeRule compilerGeneratedDelegate) {
            CompilerGeneratedDelegate = compilerGeneratedDelegate;
        }

        [NotNull]
        public ApiRules Namespace([NotNull] string @namespace, ApiAccess access, [CanBeNull] Action<ApiNamespaceRule> setup = null) {
            Argument.NotNullOrEmpty(nameof(@namespace), @namespace);

            if (!_namespaces.TryGetValue(@namespace, out var rule)) {
                rule = new ApiNamespaceRule();
                _namespaces.Add(@namespace, rule);
            }
            rule.Access = access;
            setup?.Invoke(rule);
            return this;
        }

        public ApiTypeRule CompilerGeneratedDelegate { get; }
        public IReadOnlyDictionary<string, ApiNamespaceRule> Namespaces => (IReadOnlyDictionary<string, ApiNamespaceRule>)_namespaces;
    }
}
