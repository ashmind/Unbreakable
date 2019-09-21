using System.Reflection;
using CommandLine;

namespace Unbreakable.Tools.PolicyReport {
    public class Arguments {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [Option("assembly", Required = true)]
        public string PolicyFactoryAssemblyName { get; set; }

        [Option("factory", Required = true)]
        public string PolicyFactoryTypeName { get; set; }

        [Option("method", Required = true)]
        public string PolicyFactoryMethodName { get; set; }

        [Option("output", Required = true)]
        public string OutputPath { get; set; }
        #pragma warning restore CS8618 // Non-nullable field is uninitialized.
    }
}