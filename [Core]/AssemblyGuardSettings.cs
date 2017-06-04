using System.Collections.Generic;
using Unbreakable.Internal;

namespace Unbreakable {
    public class AssemblyGuardSettings {
        public static AssemblyGuardSettings Default { get; } = new AssemblyGuardSettings();

        public AssemblyGuardSettings() {
            Filter = new ApiFilter();
        }

        public IApiFilterSettings Filter { get; }
    }
}