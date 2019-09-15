using System;

namespace Unbreakable.Demo.Models {
    public class DemoViewModel {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public string Code { get; set; }
        #pragma warning restore CS8618 // Non-nullable field is uninitialized.
        public ResultViewModel? Result { get; set; }
    }
}