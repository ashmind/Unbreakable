using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class MemoryGuardException : GuardException {
        public MemoryGuardException() { }
        public MemoryGuardException(string message) : base(message) { }
        public MemoryGuardException(string message, Exception inner) : base(message, inner) { }
        protected MemoryGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
