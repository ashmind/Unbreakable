using System;

namespace Unbreakable.Runtime.Internal {
    internal interface IRuntimeGuardSettings {
        int StackBytesLimit { get; }
        TimeSpan TimeLimit { get; }
    }
}
