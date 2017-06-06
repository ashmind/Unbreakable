using System.Reflection;
using JetBrains.Annotations;

namespace Unbreakable {
    public interface IApiFilter {
        ApiFilterResult Filter([NotNull] string @namespace, [NotNull] string typeName, ApiFilterTypeKind typeKind, [CanBeNull] string memberName = null);
    }
}
