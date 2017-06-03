using System;
using System.Diagnostics;
using System.Threading;

namespace Unbreakable.Runtime {
    public class RuntimeGuard {
        private const long Temp_AllowedTicks = 500 * TimeSpan.TicksPerMillisecond;

        private bool _enabled;

        private long _stackBaseline;
        private Stopwatch _stopwatch;

        public RuntimeGuard() {
            _stopwatch = new Stopwatch();
        }

        public void GuardEnter() {
            EnsureEnabled();
            EnsureStack();
            EnsureTime();
        }

        public void GuardJump() {
            EnsureEnabled();
            EnsureTime();
        }

        private void EnsureStack() {
            var stackCurrent = GetCurrentLocationInStack();
            if (_stackBaseline == 0)
                Interlocked.CompareExchange(ref _stackBaseline, stackCurrent, 0);

            if (_stackBaseline - stackCurrent > 10000)
                throw new StackLimitException("Stack limit reached.");
        }

        private void EnsureTime() {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
            if (_stopwatch.ElapsedTicks > Temp_AllowedTicks)
                throw new TimeLimitException("Time limit reached.");
        }

        private void EnsureEnabled() {
            if (!_enabled)
                throw new GuardException(GuardException.NoScopeMessage);
        }

        private unsafe long GetCurrentLocationInStack() {
            byte* local = stackalloc byte[1];
            return (long)local;
        }

        internal void Reset() {
            _stackBaseline = 0;
            _enabled = true;
            _stopwatch.Stop();
            _stopwatch.Reset();
        }

        internal void Disable() {
            _enabled = false;
        }
    }
}