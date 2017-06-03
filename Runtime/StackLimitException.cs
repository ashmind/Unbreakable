using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class StackLimitException : GuardException {
        public StackLimitException() {}
        public StackLimitException(string message) : base(message) {}
        public StackLimitException(string message, Exception inner) : base(message, inner) {}
        protected StackLimitException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
