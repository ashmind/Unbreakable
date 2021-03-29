using System;
using System.Collections.Generic;
using System.Threading;
using Unbreakable.Policy.Internal;

namespace Unbreakable.Policy {
    public class MemberPolicy {
        // Strict mode that blocks read access to Rewriters without call to HasRewriters.
        // This is only used in test mode for now.
        internal static bool MustVerifyRewritersAllocations { get; set; }

        private ApiAccess _access;
        private IList<IMemberRewriterInternal>? _rewriters;

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

        public bool HasRewriters => _rewriters != null && _rewriters.Count > 0;

        public IReadOnlyCollection<IMemberRewriter> Rewriters {
            get {
                EnsureRewriters(read: true);
                return (IReadOnlyCollection<IMemberRewriter>)_rewriters!;
            }
        }

        internal IReadOnlyCollection<IMemberRewriterInternal> InternalRewriters => (IReadOnlyCollection<IMemberRewriterInternal>)Rewriters;

        public MemberPolicy AddRewriter(IMemberRewriter rewriter) {
            Argument.NotNull("rewriter", rewriter);
            EnsureRewriters();
            _rewriters!.Add(Argument.Cast<IMemberRewriterInternal>(nameof(rewriter), rewriter));
            return this;
        }

        public MemberPolicy RemoveRewriter(IMemberRewriter rewriter) {
            EnsureRewriters();
            _rewriters!.Remove((IMemberRewriterInternal)rewriter);
            return this;
        }

        private void EnsureRewriters(bool read = false) {
            if (_rewriters == null) {
                if (read && MustVerifyRewritersAllocations)
                    throw new InvalidOperationException($"Detected read access to {nameof(Rewriters)} without check for {nameof(HasRewriters)}.");
                Interlocked.CompareExchange(ref _rewriters, new List<IMemberRewriterInternal>(), null);
            }
        }
    }
}
