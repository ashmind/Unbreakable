namespace Unbreakable.Roslyn {
    public class RoslynGuardSettings {
        internal static RoslynGuardSettings Default { get; } = new RoslynGuardSettings();

        public int OpenCurlyBracketCountLimit { get; set; } = 200;
        public int OpenRoundBracketCountLimit { get; set; } = 400;
        public int OpenSquareBracketCountLimit { get; set; } = 100;
        public int DotCountLimit { get; set; } = 500;
    }
}
