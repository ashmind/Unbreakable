using System;
using System.Collections.Generic;
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
        [ThreadStatic] private static long _staticConstructorStackBaseline;

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

        public void GuardEnterStaticConstructor() {
            EnsureActive();
            _staticConstructorStackBaseline = GetCurrentStackOffset();
            EnsureTime();
        }

        public void GuardExitStaticConstructor() {
            _staticConstructorStackBaseline = 0;
        }

        public void GuardJump() {
            EnsureActive();
            EnsureTime();
        }

        public IEnumerable<T> GuardCollectedEnumerable<T>(IEnumerable<T> enumerable) {
            EnsureActive();
            foreach (var item in enumerable) {
                EnsureTime();
                EnsureCount(1);
                yield return item;
            }
        }

        public IEnumerable<T> GuardIteratedEnumerable<T>(IEnumerable<T> enumerable) {
            EnsureActive();
            foreach (var item in enumerable) {
                EnsureTime();
                yield return item;
            }
        }

        private void GuardCount(long count) {
            EnsureActive();
            EnsureTime();
            EnsureCount(count);
        }

        private void EnsureStack() {
            var stackCurrent = GetCurrentStackOffset();

            long stackBaseline;
            if (_staticConstructorStackBaseline != 0) {
                stackBaseline = _staticConstructorStackBaseline;
            }
            else {
                if (_stackBaseline == 0)
                    Interlocked.CompareExchange(ref _stackBaseline, stackCurrent, 0);
                stackBaseline = _stackBaseline;
            }

            if (stackBaseline - stackCurrent > _stackBytesLimit)
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

        private void EnsureCount(long count) {
            Interlocked.Add(ref _allocatedCountTotal, count);
            if (_allocatedCountTotal > _allocatedCountTotalLimit)
                throw new MemoryGuardException("Total allocation limit reached (collections and strings).");
        }

        private unsafe long GetCurrentStackOffset() {
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

        public static class FlowThrough {
            public static IEnumerable<T> GuardCollectedEnumerable<T>(IEnumerable<T> enumerable, RuntimeGuard guard) {
                return guard.GuardCollectedEnumerable(enumerable);
            }

            public static IEnumerable<T> GuardIteratedEnumerable<T>(IEnumerable<T> enumerable, RuntimeGuard guard) {
                return guard.GuardIteratedEnumerable(enumerable);
            }

            public static IntPtr GuardCountForIntPtr(IntPtr count, RuntimeGuard guard) {
                guard.GuardCount(count.ToInt64());
                return count;
            }

            public static int GuardCountForInt32(int count, RuntimeGuard guard) {
                guard.GuardCount(count);
                return count;
            }

            public static long GuardCountForInt64(long count, RuntimeGuard guard) {
                guard.GuardCount(count);
                return count;
            }
        }
    }
}