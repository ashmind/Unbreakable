using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unbreakable.Policy;
using Unbreakable.Policy.Internal;

namespace Unbreakable {
    public class ApiPolicy {
        private static readonly IDefaultApiPolicyFactory DefaultFactory = (IDefaultApiPolicyFactory)Activator.CreateInstance(
            Type.GetType("Unbreakable.Policy.Internal.DefaultApiPolicyFactory, Unbreakable.Policy", true)
        );
        private readonly IDictionary<string, NamespacePolicy> _namespaces = new Dictionary<string, NamespacePolicy>();

        [NotNull]
        public static ApiPolicy SafeDefault() => DefaultFactory.CreateSafeDefaultPolicy();

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
