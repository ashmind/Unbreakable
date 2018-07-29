using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class RateGuardException : GuardException {
        internal RateGuardException() : base("Operation limit reached.") {}
        protected RateGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}