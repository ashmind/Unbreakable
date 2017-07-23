using System;
using System.Text.RegularExpressions;
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

        public int MethodLocalsSizeLimit { get; set; }
        public int MethodStackPushSizeLimit { get; set; }
        public Regex AllowExplicitLayoutInTypesMatchingPattern { get; set; }
    }
}