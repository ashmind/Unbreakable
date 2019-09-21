using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Unbreakable.Internal;

namespace Unbreakable {
    public class AssemblyGuardSettings {
        internal static AssemblyGuardSettings Default { get; } = new AssemblyGuardSettings();

        private ApiFilter _apiFilter;

        public AssemblyGuardSettings() {
            _apiFilter = new ApiFilter(ApiPolicy.SafeDefault());
            MethodLocalsSizeLimit = IntPtr.Size * 32;
            MethodStackPushSizeLimit = 64;
        }

        public IApiFilter ApiFilter => _apiFilter;

        public ApiPolicy ApiPolicy {
            get => _apiFilter.Policy;
            set => _apiFilter.Policy = Argument.NotNull(nameof(value), value);
        }

        public int MethodLocalsSizeLimit { get; set; }
        public int MethodStackPushSizeLimit { get; set; }
        public Regex? AllowExplicitLayoutInTypesMatchingPattern { get; set; }
        public Regex? AllowPointerOperationsInTypesMatchingPattern { get; set; }

        [Obsolete("Users are now allowed to create types in System namespace (except for a few well-known types). This property will be removed in the next version.")]
        public Regex? AllowCustomTypesMatchingPatternInSystemNamespaces { get; set; }

        public static AssemblyGuardSettings DefaultForCSharpAssembly() {
            return new AssemblyGuardSettings {
                // Array initializers for constant arrays use those
                AllowExplicitLayoutInTypesMatchingPattern = new Regex("<PrivateImplementationDetails>"),
                // Anonymous types use pointer operations in ToString()
                AllowPointerOperationsInTypesMatchingPattern = new Regex("^<>f__AnonymousType.+$"),
                #pragma warning disable CS0618 // Type or member is obsolete
                AllowCustomTypesMatchingPatternInSystemNamespaces = new Regex("_TO_BE_REMOVED_")
                #pragma warning restore CS0618 // Type or member is obsolete
            };
        }
    }
}