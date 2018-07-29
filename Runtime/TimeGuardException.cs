using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class TimeGuardException : GuardException {
        internal TimeGuardException() : base("Time limit reached.") {}
        internal TimeGuardException(Exception innerException) : base("Time limit reached.", innerException) { }
        internal TimeGuardException(string message) : base(message) { }
        internal TimeGuardException(string message, Exception innerException) : base(message, innerException) { }
        protected TimeGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}