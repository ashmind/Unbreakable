using System;
using System.Runtime.Serialization;

namespace Unbreakable {
    [Serializable]
    public class ApiGuardException : Exception {
        public ApiGuardException() { }
        public ApiGuardException(string message) : base(message) { }
        public ApiGuardException(string message, Exception inner) : base(message, inner) { }
        protected ApiGuardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
