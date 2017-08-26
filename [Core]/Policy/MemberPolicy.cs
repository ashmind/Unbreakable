using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy {
    public class MemberPolicy {
        private ApiAccess _access;
        private IList<IMemberRewriterInternal> _rewriters;

        internal MemberPolicy(ApiAccess access) {
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

        public bool HasRewriters => _rewriters != null;

        [NotNull]
        public IReadOnlyCollection<IMemberRewriter> Rewriters {
            get {
                EnsureRewriters();
                return (IReadOnlyCollection<IMemberRewriter>)_rewriters;
            }
        }

        [NotNull]
        internal IReadOnlyCollection<IMemberRewriterInternal> InternalRewriters => (IReadOnlyCollection<IMemberRewriterInternal>)Rewriters;

        [NotNull]
        public MemberPolicy AddRewriter([NotNull] IMemberRewriter rewriter) {
            Argument.NotNull("rewriter", rewriter);
            EnsureRewriters();
            _rewriters.Add(Argument.Cast<IMemberRewriterInternal>(nameof(rewriter), rewriter));
            return this;
        }

        [NotNull]
        public MemberPolicy RemoveRewriter([CanBeNull] IMemberRewriter rewriter) {
            EnsureRewriters();
            _rewriters.Remove((IMemberRewriterInternal)rewriter);
            return this;
        }

        private void EnsureRewriters() {
            if (_rewriters == null)
                Interlocked.CompareExchange(ref _rewriters, new List<IMemberRewriterInternal>(), null);
        }
    }
}
