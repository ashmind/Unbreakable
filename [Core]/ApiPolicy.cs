using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unbreakable.Internal;
using Unbreakable.Policy;

namespace Unbreakable {
    public class ApiPolicy {
        private readonly IDictionary<string, NamespacePolicy> _namespaces = new Dictionary<string, NamespacePolicy>();

        public static ApiPolicy SafeDefault() => SafeDefaultApiPolicy.Create();

        internal ApiPolicy(TypePolicy compilerGeneratedDelegate) {
            CompilerGeneratedDelegate = compilerGeneratedDelegate;
        }

        [NotNull]
        public ApiPolicy Namespace([NotNull] string @namespace, ApiAccess access, [CanBeNull] Action<NamespacePolicy> setup = null) {
            Argument.NotNull(nameof(@namespace), @namespace);

            if (!_namespaces.TryGetValue(@namespace, out var rule)) {
                rule = new NamespacePolicy();
                _namespaces.Add(@namespace, rule);
            }
            rule.Access = access;
            setup?.Invoke(rule);
            return this;
        }

        public TypePolicy CompilerGeneratedDelegate { get; }
        public IReadOnlyDictionary<string, NamespacePolicy> Namespaces => (IReadOnlyDictionary<string, NamespacePolicy>)_namespaces;
    }
}
