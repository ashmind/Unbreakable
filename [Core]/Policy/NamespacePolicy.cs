using System;
using System.Collections.Generic;

namespace Unbreakable.Policy {
    public class NamespacePolicy {
        private readonly IDictionary<string, TypePolicy> _types = new Dictionary<string, TypePolicy>();
        private readonly string _namespace;

        internal NamespacePolicy(string @namespace, ApiAccess access = ApiAccess.Neutral) {
            _namespace = @namespace;
            Access = access;
        }

        public NamespacePolicy Type(Type type, ApiAccess access, Action<TypePolicy>? setup = null) {
            if (type.Namespace != _namespace)
                throw new ArgumentException($"Type {type} must be configured under {type.Namespace} (and not {_namespace}).");

            var typeName = type.Name;
            if (type.IsNested)
                typeName = type.FullName.Substring(type.Namespace.Length + 1);
            return Type(typeName, access, setup);
        }

        public NamespacePolicy Type(string typeName, ApiAccess access, Action<TypePolicy>? setup = null) {
            Argument.NotNullOrEmpty(nameof(typeName), typeName);
            if (Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Type access ({access}) cannot exceed namespace access ({Access}).");

            if (!_types.TryGetValue(typeName, out var rule)) {
                rule = new TypePolicy();
                _types.Add(typeName, rule);
            }
            rule.Access = access;
            setup?.Invoke(rule);
            return this;
        }

        public ApiAccess Access { get; internal set; }
        public IReadOnlyDictionary<string, TypePolicy> Types => (IReadOnlyDictionary<string, TypePolicy>)_types;
    }
}
