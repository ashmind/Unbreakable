using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Unbreakable.Runtime.Internal {
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RuntimeGuard {
        private bool _active;

        private int _operationCountLimit;
        private long _stackBytesLimit;
        private long _stackBytesLimitInExceptionHandlers;
        private long _allocatedCountTotalLimit;
        private long _timeLimitStopwatchTicks;
        private Action<IDisposable> _afterForcedDispose;

        private long _stackBaseline;
        private readonly Stopwatch _stopwatch;
        private int _operationCount;
        private long _allocatedCountTotal;
        [ThreadStatic] private static long _staticConstructorStackBaseline;
        private HashSet<IDisposable> _disposables;

        public RuntimeGuard() {
            _stopwatch = new Stopwatch();
        }

        public void GuardEnter() {
            EnsureActive();
            EnsureStack();
            EnsureTime();
            EnsureRate();
        }

        public void GuardEnterStaticConstructor() {
            EnsureActive();
            _staticConstructorStackBaseline = GetCurrentStackOffset();
            EnsureTime();
            EnsureRate();
        }

        public void GuardExitStaticConstructor() {
            _staticConstructorStackBaseline = 0;
        }

        public void GuardJump() {
            EnsureActive();
            EnsureTime();
            EnsureRate();
        }

        public void GuardCount(long count) {
            EnsureActive();
            EnsureTime();
            EnsureCount(count);
        }

        public IEnumerable<T> GuardEnumerableCollected<T>(IEnumerable<T> enumerable) {
            EnsureActive();
            foreach (var item in enumerable) {
                EnsureTime();
                EnsureCount(1);
                EnsureRate();
                yield return item;
            }
        }

        public void CollectDisposable(IDisposable disposable) {
            if (disposable == null)
                return;
            if (_disposables == null)
                _disposables = new HashSet<IDisposable>();
            _disposables.Add(disposable);
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

            var stackBytesCount = stackBaseline - stackCurrent;
            if (stackBytesCount > _stackBytesLimit) {
                if (Marshal.GetExceptionCode() == 0)
                    throw new StackGuardException(stackBaseline, stackCurrent, _stackBytesLimit);
                // https://github.com/ashmind/SharpLab/issues/269#issuecomment-383370318
                if (stackBytesCount > _stackBytesLimitInExceptionHandlers)
                    throw new StackGuardException(stackBaseline, stackCurrent, _stackBytesLimitInExceptionHandlers);
            }
        }

        private void EnsureTime() {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
            #if DEBUG
            if (Debugger.IsAttached)
                return;
            #endif
            if (_stopwatch.ElapsedTicks > _timeLimitStopwatchTicks)
                throw new TimeGuardException();
        }

        private void EnsureRate() {
            _operationCount += 1;
            if (_operationCount > _operationCountLimit)
                throw new RateGuardException();
        }

        private void EnsureActive() {
            if (!_active)
                throw new GuardException(GuardException.NoScopeMessage);
        }

        private void EnsureCount(long count) {
            Interlocked.Add(ref _allocatedCountTotal, count);
            if (_allocatedCountTotal > _allocatedCountTotalLimit)
                throw new MemoryGuardException();
        }

        private unsafe long GetCurrentStackOffset() {
            byte* local = stackalloc byte[1];
            return (long)local;
        }

        internal void Start(IRuntimeGuardSettings settings) {
            _active = true;

            _stackBytesLimit = settings.StackBytesLimit;
            _stackBytesLimitInExceptionHandlers = settings.StackBytesLimitInExceptionHandlers;
            _allocatedCountTotalLimit = settings.AllocatedCountTotalLimit;
            _timeLimitStopwatchTicks = (long)(settings.TimeLimit.TotalSeconds * Stopwatch.Frequency);
            _operationCountLimit = settings.OperationCountLimit;
            _afterForcedDispose = settings.AfterForcedDispose;

            _stackBaseline = 0;
            _operationCount = 0;

            _disposables?.Clear();

            _stopwatch.Stop();
            _stopwatch.Reset();
        }

        internal void Stop() {
            _active = false;
            if (_disposables == null)
                return;
            foreach (var disposable in _disposables) {
                try {
                    disposable.Dispose();
                    _afterForcedDispose?.Invoke(disposable);
                }
                catch {
                }
            }
        }

        public TimeSpan GetTimeUntilLimit() {
            EnsureTime();
            return new TimeSpan(_timeLimitStopwatchTicks - _stopwatch.ElapsedTicks);
        }

        public static class FlowThrough {
            public static IntPtr GuardCountIntPtr(IntPtr count, RuntimeGuard guard) {
                guard.GuardCount(count.ToInt64());
                return count;
            }

            public static int GuardCountInt32(int count, RuntimeGuard guard) {
                guard.GuardCount(count);
                return count;
            }

            public static long GuardCountInt64(long count, RuntimeGuard guard) {
                guard.GuardCount(count);
                return count;
            }

            public static IEnumerable<T> GuardEnumerableCollected<T>(IEnumerable<T> enumerable, RuntimeGuard guard) {
                return guard.GuardEnumerableCollected(enumerable);
            }

            public static TDisposable CollectDisposable<TDisposable>(TDisposable disposable, RuntimeGuard guard)
                where TDisposable : IDisposable
            {
                guard.CollectDisposable(disposable);
                return disposable;
            }
        }
    }
}