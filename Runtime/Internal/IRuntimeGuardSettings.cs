using System;

namespace Unbreakable.Runtime.Internal {
    internal interface IRuntimeGuardSettings {
        int StackBytesLimit { get; }
        int StackBytesLimitInExceptionHandlers { get; }
        long AllocatedCountTotalLimit { get; }
        TimeSpan TimeLimit { get; }
        int OperationCountLimit { get; }
        Action<IDisposable>? AfterForcedDispose { get; }
    }
}
