using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class RateGuardException : GuardException {
        internal RateGuardException() {}
        internal RateGuardException(string message) : base(message) {}
        internal RateGuardException(string message, Exception innerException) : base(message, innerException) {}
        protected RateGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}