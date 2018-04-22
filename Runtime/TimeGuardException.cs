using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class TimeGuardException : GuardException {
        internal TimeGuardException() {}
        internal TimeGuardException(string message) : base(message) {}
        internal TimeGuardException(string message, Exception innerException) : base(message, innerException) {}
        protected TimeGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}