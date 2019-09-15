using System;
using JetBrains.Annotations;
using Unbreakable.Policy;

namespace Unbreakable.Internal {
    using static ApiFilterResultKind;

    internal class ApiFilter : IApiFilter {
        public ApiFilter(ApiPolicy policy) {
            Policy = policy;
        }

        public ApiFilterResult Filter(string @namespace, [NotNull] string typeName, ApiFilterTypeKind typeKind, string? memberName = null) {
            Argument.NotNull(nameof(@namespace), @namespace);
            Argument.NotNullOrEmpty(nameof(typeName), typeName);

            TypePolicy typeRule;
            switch (typeKind) {
                case ApiFilterTypeKind.External:
                    if (!Policy.Namespaces.TryGetValue(@namespace, out var namespaceRule) || namespaceRule.Access == ApiAccess.Denied)
                        return new ApiFilterResult(DeniedNamespace);

                    if (!namespaceRule.Types.TryGetValue(typeName, out typeRule))
                        return new ApiFilterResult(namespaceRule.Access == ApiAccess.Allowed ? Allowed : DeniedType);
                    break;
                case ApiFilterTypeKind.Array:
                    return Filter(nameof(System), nameof(Array), ApiFilterTypeKind.External, memberName);
                case ApiFilterTypeKind.CompilerGeneratedDelegate:
                    typeRule = Policy.CompilerGeneratedDelegate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeKind));
            }

            if (typeRule.Access == ApiAccess.Denied)
                return new ApiFilterResult(DeniedType);

            if (memberName == null)
                return new ApiFilterResult(Allowed);

            if (!typeRule.Members.TryGetValue(memberName, out var memberRule))
                return new ApiFilterResult(typeRule.Access == ApiAccess.Allowed ? Allowed : DeniedMember);

            if (memberRule.Access == ApiAccess.Denied)
                return new ApiFilterResult(DeniedMember, memberRule);

            return new ApiFilterResult(Allowed, memberRule);
        }

        [NotNull]
        public ApiPolicy Policy { get; set; }
    }
}
