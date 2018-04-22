using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class MemoryGuardException : GuardException {
        internal MemoryGuardException() { }
        internal MemoryGuardException(string message) : base(message) { }
        internal MemoryGuardException(string message, Exception inner) : base(message, inner) { }
        protected MemoryGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
