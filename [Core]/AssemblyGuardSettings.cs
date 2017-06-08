using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unbreakable.Internal;

namespace Unbreakable {
    public class AssemblyGuardSettings {
        [NotNull] internal static AssemblyGuardSettings Default { get; } = new AssemblyGuardSettings();

        private ApiFilter _apiFilter;

        public AssemblyGuardSettings() {
            _apiFilter = new ApiFilter(SafeDefaultApiRules.Create());
            MethodLocalsSizeLimit = IntPtr.Size * 10;
            MethodStackPushSizeLimit = 64;
        }

        [NotNull] public IApiFilter ApiFilter => _apiFilter;

        [NotNull]
        public ApiRules ApiRules {
            get => _apiFilter.Rules;
            set => _apiFilter.Rules = Argument.NotNull(nameof(value), value);
        }

        public int MethodLocalsSizeLimit { get; }
        public int MethodStackPushSizeLimit { get; }
    }
}