using System;

namespace Unbreakable.Demo.Models {
    public class ResultViewModel {
        public ResultViewModel(
            string output,
            TimeSpan compilationTime,
            TimeSpan rewriteTime,
            TimeSpan executionTime,
            TimeSpan totalTime
        ) {
            Output = output;
            CompilationTime = compilationTime;
            RewriteTime = rewriteTime;
            ExecutionTime = executionTime;
            TotalTime = totalTime;
        }

        public string Output { get; }
        public TimeSpan CompilationTime { get; }
        public TimeSpan RewriteTime { get; }
        public TimeSpan ExecutionTime { get; }
        public TimeSpan TotalTime { get; }
    }
}
