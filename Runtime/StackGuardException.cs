using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class StackGuardException : GuardException {
        public StackGuardException() {}
        public StackGuardException(string message) : base(message) {}
        public StackGuardException(string message, Exception inner) : base(message, inner) {}
        protected StackGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
