using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Unbreakable.Runtime.Internal {
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RuntimeGuard {
        private bool _active;

        private long _stackBaseline;
        private readonly Stopwatch _stopwatch;
        private long _allocatedCountTotal;

        private long _stackBytesLimit;
        private long _allocatedCountTotalLimit;
        private long _timeLimitStopwatchTicks;

        public RuntimeGuard() {
            _stopwatch = new Stopwatch();
        }

        public void GuardEnter() {
            EnsureActive();
            EnsureStack();
            EnsureTime();
        }

        public void GuardJump() {
            EnsureActive();
            EnsureTime();
        }
        
        private void GuardCount(long count) {
            EnsureActive();
            EnsureTime();
            Interlocked.Add(ref _allocatedCountTotal, count);
            if (_allocatedCountTotal > _allocatedCountTotalLimit)
                throw new MemoryGuardException("Total size limit reached for collections and strings.");
        }

        public static IntPtr GuardCountFlowThroughForIntPtr(IntPtr count, RuntimeGuard guard) {
            guard.GuardCount(count.ToInt64());
            return count;
        }

        public static int GuardCountFlowThroughForInt32(int count, RuntimeGuard guard) {
            guard.GuardCount(count);
            return count;
        }

        public static long GuardCountFlowThroughForInt64(long count, RuntimeGuard guard) {
            guard.GuardCount(count);
            return count;
        }

        private void EnsureStack() {
            var stackCurrent = GetCurrentLocationInStack();
            if (_stackBaseline == 0)
                Interlocked.CompareExchange(ref _stackBaseline, stackCurrent, 0);

            if (_stackBaseline - stackCurrent > _stackBytesLimit)
                throw new StackGuardException("Stack limit reached.");
        }

        private void EnsureTime() {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
            if (_stopwatch.ElapsedTicks > _timeLimitStopwatchTicks)
                throw new TimeGuardException($"Time limit reached.");
        }

        private void EnsureActive() {
            if (!_active)
                throw new GuardException(GuardException.NoScopeMessage);
        }

        private unsafe long GetCurrentLocationInStack() {
            byte* local = stackalloc byte[1];
            return (long)local;
        }

        internal void Start(IRuntimeGuardSettings settings) {
            _active = true;

            _stackBytesLimit = settings.StackBytesLimit;
            _allocatedCountTotalLimit = settings.AllocatedCountTotalLimit;
            _timeLimitStopwatchTicks = (long)(settings.TimeLimit.TotalSeconds * Stopwatch.Frequency);

            _stackBaseline = 0;
            _stopwatch.Stop();
            _stopwatch.Reset();
        }

        internal void Stop() {
            _active = false;
        }
    }
}