using System.Diagnostics;

namespace Unbreakable.Runtime {
    public class Guard {
        private readonly int _stackBaseline;

        public Guard() {
            _stackBaseline = new StackTrace().FrameCount;
        }

        public void Stack() {
            var stackSize = new StackTrace().FrameCount;
            if (stackSize - _stackBaseline > 10)
                throw new StackLimitException("Stack limit reached.");
        }

        public void Duration() {
        }
    }
}