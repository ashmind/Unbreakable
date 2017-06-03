using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class GuardException : Exception {
        internal const string NoScopeMessage = "Rewritten assembly can only be used within RuntimeGuardToken.Scope().";

        public GuardException() { }
        public GuardException(string message) : base(message) { }
        public GuardException(string message, Exception inner) : base(message, inner) { }
        protected GuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
