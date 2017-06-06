using System.Reflection;
using JetBrains.Annotations;

namespace Unbreakable.Internal {
    internal partial class ApiFilter : IApiFilter {
        public ApiFilter(ApiRules rules) {
            Rules = rules;
        }

        public ApiFilterResult Filter([NotNull] string @namespace, [NotNull] string typeName, [CanBeNull] string memberName = null) {
            Argument.NotNull(nameof(@namespace), @namespace);
            Argument.NotNullOrEmpty(nameof(typeName), typeName);

            if (!Rules.Namespaces.TryGetValue(@namespace, out var namespaceRule) || namespaceRule.Access == ApiAccess.Denied)
                return ApiFilterResult.DeniedNamespace;

            if (!namespaceRule.Types.TryGetValue(typeName, out var typeRule))
                return namespaceRule.Access == ApiAccess.Allowed ? ApiFilterResult.Allowed : ApiFilterResult.DeniedType;

            if (typeRule.Access == ApiAccess.Denied)
                return ApiFilterResult.DeniedType;

            if (memberName == null)
                return ApiFilterResult.Allowed;

            if (!typeRule.Members.TryGetValue(memberName, out var memberAccess))
                return typeRule.Access == ApiAccess.Allowed ? ApiFilterResult.Allowed : ApiFilterResult.DeniedMember;

            if (memberAccess == ApiAccess.Denied)
                return ApiFilterResult.DeniedMember;

            return ApiFilterResult.Allowed;
        }

        [NotNull]
        public ApiRules Rules { get; set; }
    }
}
