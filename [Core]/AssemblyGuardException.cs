using System;
using System.Runtime.Serialization;

namespace Unbreakable {
    [Serializable]
    public class AssemblyGuardException : Exception {
        public AssemblyGuardException() { }
        public AssemblyGuardException(string message) : base(message) { }
        public AssemblyGuardException(string message, Exception inner) : base(message, inner) { }
        protected AssemblyGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
