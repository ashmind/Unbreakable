using System;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    public class RuntimeGuardSettings : IRuntimeGuardSettings {
        private TimeSpan _timeLimit = TimeSpan.FromMilliseconds(500);
        private int _stackBytesLimit = IntPtr.Size >= 8 ? 2048 : 1024;
        private int _stackBytesLimitInExceptionHandlers = IntPtr.Size >= 8 ? 16384 : 6144;
        private long _allocatedCountTotalLimit = 100;
        private int _operationCountLimit = 500;

        internal static RuntimeGuardSettings Default { get; } = new RuntimeGuardSettings();

        public int StackBytesLimit {
            get => _stackBytesLimit;
            set {
                Argument.PositiveNonZero(nameof(value), value);
                _stackBytesLimit = value;
            }
        }

        public int StackBytesLimitInExceptionHandlers {
            get => _stackBytesLimitInExceptionHandlers;
            set {
                Argument.PositiveNonZero(nameof(value), value);
                _stackBytesLimitInExceptionHandlers = value;
            }
        }

        public long AllocatedCountTotalLimit {
            get => _allocatedCountTotalLimit;
            set {
                Argument.PositiveNonZero(nameof(value), value);
                _allocatedCountTotalLimit = value;
            }
        }

        public TimeSpan TimeLimit {
            get => _timeLimit;
            set {
                Argument.PositiveNonZero(nameof(value), value.Ticks);
                _timeLimit = value;
            }
        }

        public int OperationCountLimit {
            get => _operationCountLimit;
            set {
                Argument.PositiveNonZero(nameof(value), value);
                _operationCountLimit = value;
            }
        }

        public Action<IDisposable>? AfterForcedDispose { get; set; }
    }
}
