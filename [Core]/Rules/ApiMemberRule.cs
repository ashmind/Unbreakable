using System;
using JetBrains.Annotations;
using Unbreakable.Rules.Internal;

namespace Unbreakable.Rules {
    public class ApiMemberRule {
        private ApiAccess _access;
        private IApiMemberRewriterInternal _rewriter;

        internal ApiMemberRule(ApiAccess access) {
            Access = access;
        }

        public ApiAccess Access {
            get => _access;
            set {
                if (value == ApiAccess.Neutral)
                    throw new ArgumentOutOfRangeException(nameof(value), "Neutral access is not allowed at member level.");
                _access = value;
            }
        }

        [CanBeNull]
        public IApiMemberRewriter Rewriter {
            get => _rewriter;
            set {
                if (!(value is IApiMemberRewriterInternal @internal))
                    throw new ArgumentException("Rewriter must implement internal interface IApiMemberRewriterInternal. Custom rewriters are not yet supported.", nameof(value));
                _rewriter = @internal;
            }
        }

        [CanBeNull]
        internal IApiMemberRewriterInternal RewriterAsInternal => _rewriter;
    }
}
