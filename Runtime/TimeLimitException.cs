using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class TimeLimitException : GuardException {
        public TimeLimitException() {}
        public TimeLimitException(string message) : base(message) {}
        public TimeLimitException(string message, Exception innerException) : base(message, innerException) {}
        protected TimeLimitException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}