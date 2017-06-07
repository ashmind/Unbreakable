using System;
using System.Runtime.Serialization;
using Unbreakable.Runtime;

namespace Unbreakable.Roslyn {
    [Serializable]
    public class RoslynGuardException : GuardException {
        public RoslynGuardException() { }
        public RoslynGuardException(string message) : base(message) { }
        public RoslynGuardException(string message, Exception inner) : base(message, inner) { }
        protected RoslynGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
