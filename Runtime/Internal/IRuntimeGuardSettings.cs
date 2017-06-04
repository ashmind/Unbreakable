using System;

namespace Unbreakable.Runtime.Internal {
    internal interface IRuntimeGuardSettings {
        int StackBytesLimit { get; }
        int ArrayLengthLimit { get; }
        TimeSpan TimeLimit { get; }
    }
}
