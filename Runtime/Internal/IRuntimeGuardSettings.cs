using System;

namespace Unbreakable.Runtime.Internal {
    internal interface IRuntimeGuardSettings {
        int StackBytesLimit { get; }
        long AllocatedCountTotalLimit { get; }
        TimeSpan TimeLimit { get; }
        int OperationCountLimit { get; }
        Action<IDisposable> AfterForcedDispose { get; }
    }
}
