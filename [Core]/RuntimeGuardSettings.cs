using System;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    public class RuntimeGuardSettings : IRuntimeGuardSettings {
        public static RuntimeGuardSettings Default { get; } = new RuntimeGuardSettings();

        public RuntimeGuardSettings() {
            StackBytesLimit = 1024;
            TimeLimit = TimeSpan.FromMilliseconds(500);
        }

        public int StackBytesLimit { get; set; }
        public TimeSpan TimeLimit { get; set; }
    }
}
