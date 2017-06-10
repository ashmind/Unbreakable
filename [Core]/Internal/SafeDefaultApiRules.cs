using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unbreakable.Rules;
using Unbreakable.Rules.Rewriters;

namespace Unbreakable.Internal {
    using static ApiAccess;

    internal static class SafeDefaultApiRules {
        public static ApiRules Create() {
            return new ApiRules()
                .Namespace(nameof(System), Neutral, SetupSystem)
                .Namespace("System.Collections.Generic", Neutral, SetupSystemCollectionsGeneric)
                .Namespace("System.Linq", Allowed, n => n.Type(nameof(Enumerable), Allowed, t => t.Member(nameof(Enumerable.Range), Denied)))
                .Namespace("System.Diagnostics", Denied)
                .Namespace("System.IO", Denied)
                .Namespace("System.Reflection", Denied)
                .Namespace("System.Runtime", Denied)
                .Namespace("System.Runtime.InteropServices", Denied)
                .Namespace("System.Runtime.CompilerServices", Neutral, n => n.Type(nameof(CompilerGeneratedAttribute), Allowed))
                .Namespace("System.Threading", Denied)

                .Namespace("Unbreakable", Denied)
                .Namespace("Unbreakable.Runtime", Denied);
        }

        private static void SetupSystem(ApiNamespaceRule system) {
            system
                .Type(nameof(Activator), Denied)
                .Type(nameof(AppContext), Denied)
                .Type(nameof(AppDomain), Denied)
                .Type(nameof(AppDomainManager), Denied)
                .Type(nameof(Console), Denied)
                .Type(nameof(Environment), Denied)
                .Type(nameof(GC), Denied)
                .Type(nameof(LocalDataStoreSlot), Denied)
                .Type(nameof(OperatingSystem), Denied)
                .Type(nameof(TypedReference), Denied)

                .Type(nameof(Object), Allowed)
                .Type(nameof(String), Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCount))
                .Type(nameof(DateTime), Allowed)
                .Type(nameof(DateTimeKind), Allowed)
                .Type(nameof(DateTimeOffset), Allowed)
                .Type(typeof(void).Name, Allowed)
                .Type(nameof(Nullable), Allowed)
                .Type(typeof(Nullable<>).Name, Allowed);

            foreach (var type in PrimitiveTypes.List) {
                if (type == typeof(IntPtr) || type == typeof(UIntPtr))
                    continue;
                system.Type(type.Name, Allowed);
            }
        }

        private static void SetupSystemCollectionsGeneric(ApiNamespaceRule collections) {
            collections
                .Type(typeof(System.Collections.Generic.Comparer<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.Dictionary<,>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.EqualityComparer<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.HashSet<>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.ICollection<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IComparer<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IDictionary<,>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IEnumerable<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IEnumerator<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IEqualityComparer<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IList<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IReadOnlyCollection<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IReadOnlyDictionary<,>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.IReadOnlyList<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.ISet<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.KeyNotFoundException).Name, Allowed)
                .Type(typeof(System.Collections.Generic.KeyValuePair<,>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.LinkedList<>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.LinkedListNode<>).Name, Allowed)
                .Type(typeof(System.Collections.Generic.List<>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.Queue<>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.SortedDictionary<,>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.SortedList<,>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.SortedSet<>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity))
                .Type(typeof(System.Collections.Generic.Stack<>).Name, Allowed, t => t.Constructor(Allowed, CountMemoryGuardRewriter.ForCapacity));
        }

        internal static ApiTypeRule CreateForCompilerGeneratedDelegate() {
            return new ApiTypeRule(ApiAccess.Neutral)
                .Member(".ctor", ApiAccess.Allowed)
                .Member("Invoke", ApiAccess.Allowed);
        }
    }
}
