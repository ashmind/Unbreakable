using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class TimeGuardException : GuardException {
        public TimeGuardException() {}
        public TimeGuardException(string message) : base(message) {}
        public TimeGuardException(string message, Exception innerException) : base(message, innerException) {}
        protected TimeGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}