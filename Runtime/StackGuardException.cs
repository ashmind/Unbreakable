using System;
using System.Runtime.Serialization;

namespace Unbreakable.Runtime {
    [Serializable]
    public class StackGuardException : GuardException {
        internal StackGuardException(
            long stackBaseline,
            long stackOffset,
            long stackLimit,
            string message = "Stack limit reached.",
            Exception inner = null
        ) : base(message, inner) {
            StackBaseline = stackBaseline;
            StackOffset = stackOffset;
            StackLimit = stackLimit;
        }

        protected StackGuardException(SerializationInfo info, StreamingContext context) : base(info, context) {
            StackBaseline = info.GetInt64(nameof(StackBaseline));
            StackOffset = info.GetInt64(nameof(StackOffset));
            StackLimit = info.GetInt64(nameof(StackLimit));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(StackBaseline), StackBaseline);
            info.AddValue(nameof(StackOffset), StackOffset);
            info.AddValue(nameof(StackLimit), StackLimit);
            base.GetObjectData(info, context);
        }

        internal long StackBaseline { get; }
        internal long StackOffset { get; }

        internal long StackSize => StackBaseline - StackOffset;
        internal long StackLimit { get; }
    }
}
