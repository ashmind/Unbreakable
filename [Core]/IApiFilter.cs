using System.Reflection;
using JetBrains.Annotations;

namespace Unbreakable {
    public interface IApiFilter {
        ApiFilterResult Filter(string @namespace, string typeName, ApiFilterTypeKind typeKind, string? memberName = null);
    }
}
