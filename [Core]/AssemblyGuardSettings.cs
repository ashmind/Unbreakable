using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unbreakable.Internal;

namespace Unbreakable {
    public class AssemblyGuardSettings {
        public static AssemblyGuardSettings Default { get; } = new AssemblyGuardSettings();

        public AssemblyGuardSettings() {
            Filter = new ApiFilter();
            MethodLocalsSizeLimit = Marshal.SizeOf<int>() * 10;
        }

        public IApiFilterSettings Filter { get; }
        public int MethodLocalsSizeLimit { get; }
    }
}