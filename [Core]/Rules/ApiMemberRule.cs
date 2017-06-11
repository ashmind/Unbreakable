using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Unbreakable.Rules.Internal;

namespace Unbreakable.Rules {
    public class ApiMemberRule {
        private ApiAccess _access;
        private IList<IApiMemberRewriterInternal> _rewriters;

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

        [NotNull]
        public IReadOnlyCollection<IApiMemberRewriter> Rewriters {
            get {
                EnsureRewriters();
                return (IReadOnlyCollection<IApiMemberRewriter>)_rewriters;
            }
        }

        [NotNull]
        internal IReadOnlyCollection<IApiMemberRewriterInternal> InternalRewriters => (IReadOnlyCollection<IApiMemberRewriterInternal>)Rewriters;

        [NotNull]
        public ApiMemberRule AddRewriter([NotNull] IApiMemberRewriter rewriter) {
            Argument.NotNull("rewriter", rewriter);
            EnsureRewriters();
            _rewriters.Add(Argument.Cast<IApiMemberRewriterInternal>(nameof(rewriter), rewriter));
            return this;
        }

        [NotNull]
        public ApiMemberRule RemoveRewriter([CanBeNull] IApiMemberRewriter rewriter) {
            EnsureRewriters();
            _rewriters.Remove((IApiMemberRewriterInternal)rewriter);
            return this;
        }

        private void EnsureRewriters() {
            if (_rewriters == null)
                Interlocked.CompareExchange(ref _rewriters, new List<IApiMemberRewriterInternal>(), null);
        }
    }
}
