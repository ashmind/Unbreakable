using System;
using JetBrains.Annotations;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    public class RuntimeGuardSettings : IRuntimeGuardSettings {
        [NotNull] internal static RuntimeGuardSettings Default { get; } = new RuntimeGuardSettings();

        public int StackBytesLimit { get; set; } = IntPtr.Size >= 8 ? 2048 : 1024;
        public int StackBytesLimitInExceptionHandlers { get; set; } = IntPtr.Size >= 8 ? 16384 : 6144;
        public long AllocatedCountTotalLimit { get; set; } = 100;
        public TimeSpan TimeLimit { get; set; } = TimeSpan.FromMilliseconds(500);
        public int OperationCountLimit { get; set; } = 500;

        public Action<IDisposable>? AfterForcedDispose { get; set; }
    }
}
