using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class RateGuardException : GuardException {
        public RateGuardException() {}
        public RateGuardException(string message) : base(message) {}
        public RateGuardException(string message, Exception innerException) : base(message, innerException) {}
        protected RateGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}