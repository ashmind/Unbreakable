using System;
using JetBrains.Annotations;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    public class RuntimeGuardSettings : IRuntimeGuardSettings {
        [NotNull] internal static RuntimeGuardSettings Default { get; } = new RuntimeGuardSettings();

        public RuntimeGuardSettings() {
            StackBytesLimit = 1024;
            AllocatedCountTotalLimit = 100;
            TimeLimit = TimeSpan.FromMilliseconds(500);
            OperationCountLimit = 500;
        }

        public int StackBytesLimit { get; set; }
        public long AllocatedCountTotalLimit { get; set; }
        public TimeSpan TimeLimit { get; set; }
        public int OperationCountLimit { get; set; }

        public Action<IDisposable> AfterForcedDispose { get; set; }
    }
}
