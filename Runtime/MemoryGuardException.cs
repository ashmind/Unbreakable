using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class MemoryGuardException : GuardException {
        internal MemoryGuardException() : base("Total allocation limit reached (collections and strings).") { }
        protected MemoryGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
