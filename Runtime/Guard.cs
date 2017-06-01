using System;
using System.Diagnostics;
using System.Threading;

namespace Unbreakable.Runtime {
    public class Guard {
        private long _stackBaseline;

        public Guard() {
        }

        public void Stack() {
            var stackCurrent = GetCurrentLocationInStack();
            if (_stackBaseline == 0)
                Interlocked.CompareExchange(ref _stackBaseline, stackCurrent, 0);

            if (_stackBaseline - stackCurrent > 10000)
                throw new StackLimitException("Stack limit reached.");
        }

        public void Duration() {

        }

        private unsafe long GetCurrentLocationInStack() {
            byte* local = stackalloc byte[1];
            return (long)local;
        }
    }
}