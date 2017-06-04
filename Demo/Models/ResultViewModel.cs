using System;

namespace Unbreakable.Demo.Models {
    public class ResultViewModel {
        public ResultViewModel(string output, TimeSpan compilationTime, TimeSpan executionTime, TimeSpan totalTime) {
            Output = output;
            CompilationTime = compilationTime;
            ExecutionTime = executionTime;
            TotalTime = totalTime;
        }

        public string Output { get; }
        public TimeSpan CompilationTime { get; }
        public TimeSpan ExecutionTime { get; }
        public TimeSpan TotalTime { get; }
    }
}
