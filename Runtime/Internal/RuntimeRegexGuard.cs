using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Unbreakable.Runtime.Internal {
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RuntimeRegexGuard {
        public static void GuardOptions(RegexOptions options) {
            if ((options & RegexOptions.Compiled) == RegexOptions.Compiled)
                throw new GuardException("Option RegexOptions.Compiled is not allowed.");
        }

        public static class FlowThrough {
            public static RegexOptions GuardOptions(RegexOptions options) {
                RuntimeRegexGuard.GuardOptions(options);
                return options;
            }
        }
    }
}
