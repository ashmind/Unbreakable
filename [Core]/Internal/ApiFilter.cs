using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace Unbreakable.Internal {
    internal partial class ApiFilter : IApiFilter, IApiFilterSettings {
        private readonly IDictionary<string, NamespaceRule> _rules = GetDefaultRules();

        public ApiFilterResult Filter([NotNull] string @namespace, [NotNull] string typeName, [CanBeNull] string memberName = null, MemberTypes memberType = 0) {
            Argument.NotNull(nameof(@namespace), @namespace);
            Argument.NotNullOrEmpty(nameof(typeName), typeName);

            if (!_rules.TryGetValue(@namespace, out var namespaceRule) || namespaceRule.Access == ApiAccess.Denied)
                return ApiFilterResult.DeniedNamespace;

            if (!namespaceRule.TypeRules.TryGetValue(typeName, out var typeRule))
                return namespaceRule.Access == ApiAccess.Allowed ? ApiFilterResult.Allowed : ApiFilterResult.DeniedType;

            if (typeRule.Access == ApiAccess.Denied)
                return ApiFilterResult.DeniedType;

            if (memberName == null)
                return ApiFilterResult.Allowed;

            if (!typeRule.MemberRules.TryGetValue(memberName, out var memberAccess))
                return typeRule.Access == ApiAccess.Allowed ? ApiFilterResult.Allowed : ApiFilterResult.DeniedMember;

            if (memberAccess == ApiAccess.Denied)
                return ApiFilterResult.DeniedMember;

            return ApiFilterResult.Allowed;
        }

        public void SetupNamespace(string @namespace, ApiAccess access) {
            if (!_rules.TryGetValue(@namespace, out var rule)) {
                rule = new NamespaceRule();
                _rules.Add(@namespace, rule);
            }
            rule.Access = access;
        }

        public void SetupType(string @namespace, string typeName, ApiAccess access) {
            if (!_rules.TryGetValue(@namespace, out var namespaceRule)) {
                namespaceRule = new NamespaceRule { Access = ApiAccess.Neutral };
                _rules.Add(@namespace, namespaceRule);
            }

            if (namespaceRule.Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Cannot set access on {typeName} to {access} while parent namespace {@namespace} is Denied. Use {nameof(SetupNamespace)}() before calling this method.");

            if (!namespaceRule.TypeRules.TryGetValue(typeName, out var typeRule)) {
                typeRule = new TypeRule();
                namespaceRule.TypeRules.Add(typeName, typeRule);
            }
            typeRule.Access = access;
        }

        public void SetupMember(string @namespace, string typeName, string memberName, ApiAccess access) {
            if (!_rules.TryGetValue(@namespace, out var namespaceRule)) {
                namespaceRule = new NamespaceRule { Access = ApiAccess.Neutral };
                _rules.Add(@namespace, namespaceRule);
            }

            if (namespaceRule.Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Cannot set access on {typeName}.{memberName} to {access} while parent namespace {@namespace} is Denied. Use {nameof(SetupNamespace)}() before calling this method.");

            if (!namespaceRule.TypeRules.TryGetValue(typeName, out var typeRule)) {
                typeRule = new TypeRule { Access = ApiAccess.Neutral };
                namespaceRule.TypeRules.Add(typeName, typeRule);
            }

            if (typeRule.Access == ApiAccess.Denied && access != ApiAccess.Denied)
                throw new InvalidOperationException($"Cannot set access on {typeName}.{memberName} to {access} while parent namespace {typeName} is Denied. Use {nameof(SetupType)}() before calling this method.");

            typeRule.MemberRules[memberName] = access;
        }

        private class NamespaceRule {
            public NamespaceRule(ApiAccess access = ApiAccess.Neutral) {
                Access = access;
            }

            public ApiAccess Access { get; set; }
            public IDictionary<string, TypeRule> TypeRules { get; } = new Dictionary<string, TypeRule>();
        }

        private class TypeRule {
            public TypeRule(ApiAccess access = ApiAccess.Neutral) {
                Access = access;
            }

            public ApiAccess Access { get; set; }
            public IDictionary<string, ApiAccess> MemberRules { get; } = new Dictionary<string, ApiAccess>();
        }
    }
}
