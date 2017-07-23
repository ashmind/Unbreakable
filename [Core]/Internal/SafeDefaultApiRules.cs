using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unbreakable.Rules;
using Unbreakable.Rules.Rewriters;

namespace Unbreakable.Internal {
    using System.Diagnostics;
    using static ApiAccess;

    internal static class SafeDefaultApiRules {
        private static readonly IReadOnlyCollection<Type> DelegateTypes =
            typeof(Func<>).Assembly.GetTypes().Where(t => t.Namespace == nameof(System) && t.BaseType == typeof(MulticastDelegate)).ToArray();

        public static ApiRules Create() {
            return new ApiRules(CreateTypeRuleForCompilerGeneratedDelegate())
                .Namespace(nameof(System), Neutral, SetupSystem)
                .Namespace("System.Collections.Generic", Neutral, SetupSystemCollectionsGeneric)
                .Namespace("System.Linq", Neutral, SetupSystemLinq)
                .Namespace("System.Diagnostics", Neutral, SetupSystemDiagnostics)
                .Namespace("System.IO", Denied)
                .Namespace("System.Reflection", Denied)
                .Namespace("System.Runtime", Denied)
                .Namespace("System.Runtime.InteropServices", Denied)
                .Namespace("System.Runtime.CompilerServices", Neutral, SetupSystemRuntimeCompilerServices)
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
                .Type(nameof(GC), Denied)
                .Type(nameof(LocalDataStoreSlot), Denied)
                .Type(nameof(OperatingSystem), Denied)
                .Type(nameof(TypedReference), Denied)

                .Type(nameof(Environment), Neutral,
                    t => t.Getter(nameof(Environment.CurrentManagedThreadId), Allowed)
                )
                .Type(nameof(NotSupportedException), Neutral,
                    t => t.Constructor(Allowed)
                )

                .Type(nameof(Object), Allowed)
                .Type(nameof(String), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.Default)
                          .Member(nameof(string.Join), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(string.Concat), Allowed, EnumerableArgumentRewriter.Collected)
                )
                .Type(nameof(DateTime), Allowed)
                .Type(nameof(DateTimeKind), Allowed)
                .Type(nameof(DateTimeOffset), Allowed)
                .Type(typeof(void).Name, Allowed)
                .Type(nameof(Nullable), Allowed)
                .Type(typeof(Nullable<>).Name, Allowed)
                .Type(typeof(Random).Name, Allowed);

            foreach (var type in PrimitiveTypes.List) {
                if (type == typeof(IntPtr) || type == typeof(UIntPtr))
                    continue;
                system.Type(type.Name, Allowed);
            }

            foreach (var type in DelegateTypes) {
                system.Type(type.Name, Neutral, t => t.Constructor(Allowed).Member("Invoke", Allowed));
            }
        }

        private static void SetupSystemCollectionsGeneric(ApiNamespaceRule collections) {
            collections
                .Type(typeof(Comparer<>).Name, Allowed)
                .Type(typeof(Dictionary<,>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Other(SetupAdd)
                )
                .Type(typeof(EqualityComparer<>).Name, Allowed)
                .Type(typeof(HashSet<>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, EnumerableArgumentRewriter.Collected)
                          .Other(SetupSetCommon)
                )
                .Type(typeof(ICollection<>).Name, Allowed, SetupAdd)
                .Type(typeof(IComparer<>).Name, Allowed)
                .Type(typeof(IDictionary<,>).Name, Allowed, SetupAdd)
                .Type(typeof(IEnumerable<>).Name, Allowed)
                .Type(typeof(IEnumerator<>).Name, Allowed)
                .Type(typeof(IEqualityComparer<>).Name, Allowed)
                .Type(typeof(IList<>).Name, Allowed)
                .Type(typeof(IReadOnlyCollection<>).Name, Allowed)
                .Type(typeof(IReadOnlyDictionary<,>).Name, Allowed)
                .Type(typeof(IReadOnlyList<>).Name, Allowed)
                .Type(typeof(ISet<>).Name, Allowed, t => t.Other(SetupSetCommon))
                .Type(typeof(KeyNotFoundException).Name, Allowed)
                .Type(typeof(KeyValuePair<,>).Name, Allowed)
                .Type(typeof(LinkedList<>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, EnumerableArgumentRewriter.Collected)
                )
                .Type(typeof(LinkedListNode<>).Name, Allowed)
                .Type(typeof(List<>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(List<object>.AddRange), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(List<object>.InsertRange), Allowed, EnumerableArgumentRewriter.Collected)
                          .Other(SetupAdd)
                )
                .Type(typeof(Queue<>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Queue<object>.Enqueue), Allowed, AddCallRewriter.Default)
                )
                .Type(typeof(SortedDictionary<,>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Other(SetupAdd)
                )
                .Type(typeof(SortedList<,>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Other(SetupAdd)
                )
                .Type(typeof(SortedSet<>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, EnumerableArgumentRewriter.Collected)
                          .Other(SetupSetCommon)
                )
                .Type(typeof(Stack<>).Name, Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Stack<object>.Push), Allowed, AddCallRewriter.Default)
                );
        }

        private static void SetupSetCommon(ApiTypeRule set) {
            set.Member(nameof(ISet<object>.UnionWith), Allowed, EnumerableArgumentRewriter.Collected)
               .Member(nameof(ISet<object>.IntersectWith), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.ExceptWith), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.SymmetricExceptWith), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.IsSubsetOf), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.IsProperSubsetOf), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.IsSupersetOf), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.IsProperSupersetOf), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.Overlaps), Allowed, EnumerableArgumentRewriter.Iterated)
               .Member(nameof(ISet<object>.SetEquals), Allowed, EnumerableArgumentRewriter.Iterated)
               .Other(SetupAdd);
        }

        private static void SetupAdd(ApiTypeRule type) {
            type.Member("Add", Allowed, AddCallRewriter.Default);
        }

        private static void SetupSystemDiagnostics(ApiNamespaceRule diagnostics) {
            diagnostics
                .Type(nameof(DebuggerHiddenAttribute), Allowed)
                .Type(nameof(Process), Denied);
        }

        private static void SetupSystemLinq(ApiNamespaceRule linq) {
            linq
                .Type(nameof(Enumerable), Neutral,
                    t => t.Member(nameof(Enumerable.Aggregate), Allowed)
                          .Member(nameof(Enumerable.All), Allowed)
                          .Member(nameof(Enumerable.Any), Allowed)
                          .Member(nameof(Enumerable.AsEnumerable), Allowed)
                          .Member(nameof(Enumerable.Average), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Cast), Allowed)
                          .Member(nameof(Enumerable.Concat), Allowed)
                          .Member(nameof(Enumerable.Contains), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Count), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.DefaultIfEmpty), Allowed)
                          .Member(nameof(Enumerable.Distinct), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.ElementAt), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.ElementAtOrDefault), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Empty), Allowed)
                          .Member(nameof(Enumerable.Except), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.First), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.FirstOrDefault), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.GroupBy), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.GroupJoin), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Intersect), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Join), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Last), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.LastOrDefault), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.LongCount), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Max), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Min), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.OfType), Allowed)
                          .Member(nameof(Enumerable.OrderBy), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.OrderByDescending), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Range), Allowed)
                          .Member(nameof(Enumerable.Repeat), Allowed)
                          .Member(nameof(Enumerable.Reverse), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Select), Allowed)
                          .Member(nameof(Enumerable.SelectMany), Allowed)
                          .Member(nameof(Enumerable.SequenceEqual), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Single), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.SingleOrDefault), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Skip), Allowed)
                          .Member(nameof(Enumerable.SkipWhile), Allowed)
                          .Member(nameof(Enumerable.Sum), Allowed, EnumerableArgumentRewriter.Iterated)
                          .Member(nameof(Enumerable.Take), Allowed)
                          .Member(nameof(Enumerable.TakeWhile), Allowed)
                          .Member(nameof(Enumerable.ThenBy), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.ThenByDescending), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.ToArray), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.ToDictionary), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.ToList), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.ToLookup), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Union), Allowed, EnumerableArgumentRewriter.Collected)
                          .Member(nameof(Enumerable.Where), Allowed)
                          .Member(nameof(Enumerable.Zip), Allowed)
                );
        }

        private static void SetupSystemRuntimeCompilerServices(ApiNamespaceRule compilerServices) {
            compilerServices
                .Type(nameof(CompilerGeneratedAttribute), Allowed)
                .Type(nameof(IteratorStateMachineAttribute), Allowed);
        }

        private static ApiTypeRule CreateTypeRuleForCompilerGeneratedDelegate() {
            return new ApiTypeRule(Neutral)
                .Constructor(Allowed)
                .Member("Invoke", Allowed);
        }
    }
}
