using System;
using JetBrains.Annotations;
using Unbreakable.Rules;

namespace Unbreakable.Internal {
    internal partial class ApiFilter : IApiFilter {
        public ApiFilter(ApiRules rules) {
            Rules = rules;
        }

        public ApiFilterResult Filter([NotNull] string @namespace, [NotNull] string typeName, ApiFilterTypeKind typeKind, [CanBeNull] string memberName = null) {
            Argument.NotNull(nameof(@namespace), @namespace);
            Argument.NotNullOrEmpty(nameof(typeName), typeName);

            TypeApiRule typeRule;
            switch (typeKind) {
                case ApiFilterTypeKind.External:
                    if (!Rules.Namespaces.TryGetValue(@namespace, out var namespaceRule) || namespaceRule.Access == ApiAccess.Denied)
                        return ApiFilterResult.DeniedNamespace;

                    if (!namespaceRule.Types.TryGetValue(typeName, out typeRule))
                        return namespaceRule.Access == ApiAccess.Allowed ? ApiFilterResult.Allowed : ApiFilterResult.DeniedType;
                    break;
                case ApiFilterTypeKind.CompilerGeneratedDelegate:
                    typeRule = Rules.CompilerGeneratedDelegate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeKind));
            }

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
