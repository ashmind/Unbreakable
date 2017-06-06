using System.Reflection;
using JetBrains.Annotations;

namespace Unbreakable {
    public interface IApiFilter {
        ApiFilterResult Filter([NotNull] string @namespace, [NotNull] string typeName, [CanBeNull] string memberName = null);
    }
}
