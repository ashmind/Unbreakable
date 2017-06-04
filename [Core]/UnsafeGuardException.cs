using System;
using System.Runtime.Serialization;

namespace Unbreakable {
    [Serializable]
    public class UnsafeGuardException : Exception {
        public UnsafeGuardException() { }
        public UnsafeGuardException(string message) : base(message) { }
        public UnsafeGuardException(string message, Exception inner) : base(message, inner) { }
        protected UnsafeGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
