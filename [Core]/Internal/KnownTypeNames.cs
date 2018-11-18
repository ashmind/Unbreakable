using System;
using System.Collections.Generic;
using System.Linq;

namespace Unbreakable.Internal {
    internal static class KnownTypeNames {
        public static TypeName Object { get; } = new TypeName(typeof(Object));
        public static TypeName Delegate { get; } = new TypeName(typeof(Delegate));
        public static TypeName MulticastDelegate { get; } = new TypeName(typeof(MulticastDelegate));
        public static TypeName IEnumerable { get; } = new TypeName(typeof(IEnumerable<>));

        public static ICollection<TypeName> AllReserved { get; } = new HashSet<TypeName>(
            new[] { Object, Delegate, MulticastDelegate, IEnumerable, new TypeName(typeof(string)) }
                .Concat(PrimitiveTypes.List.Select(t => new TypeName(t)))
        );
    }
}
