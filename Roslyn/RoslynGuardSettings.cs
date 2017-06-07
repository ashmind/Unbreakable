using JetBrains.Annotations;

namespace Unbreakable.Roslyn {

    public class RoslynGuardSettings {
        [NotNull] internal static RoslynGuardSettings Default { get; } = new RoslynGuardSettings();

        public int NestingLevelLimit { get; set; } = 50;
        public int DotCountLimit { get; set; } = 100;
    }
}
