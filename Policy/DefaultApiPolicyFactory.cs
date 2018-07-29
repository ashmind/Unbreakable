using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Unbreakable.Internal;
using Unbreakable.Policy.Rewriters;

namespace Unbreakable.Policy.Internal {
    using System.Text.RegularExpressions;
    using static ApiAccess;

    internal class DefaultApiPolicyFactory : IDefaultApiPolicyFactory {
        private static readonly IReadOnlyCollection<Type> DelegateTypes =
            typeof(Func<>).Assembly.GetTypes().Where(t => t.Namespace == nameof(System) && t.BaseType == typeof(MulticastDelegate)).ToArray();

        private static readonly IReadOnlyCollection<Type> TupleTypes =
            typeof(Tuple<>).Assembly.GetTypes().Where(t => t.Namespace == nameof(System) && t.Name.StartsWith("Tuple`")).ToArray();

        private static readonly IReadOnlyCollection<string> ValueTupleTypeNames =
            Enumerable.Range(1, 8).Select(n => "ValueTuple`" + n).ToArray();

        public ApiPolicy CreateSafeDefaultPolicy() {
            return new ApiPolicy(CreateTypeRuleForCompilerGeneratedDelegate())
                .Namespace(nameof(System), Neutral, SetupSystem)
                .Namespace("System.Collections", Neutral, SetupSystemCollections)
                .Namespace("System.Collections.Generic", Neutral, SetupSystemCollectionsGeneric)
                .Namespace("System.ComponentModel", Neutral, SetupSystemComponentModel)
                .Namespace("System.Diagnostics", Neutral, SetupSystemDiagnostics)
                .Namespace("System.Globalization", Neutral, SetupSystemGlobalization)
                .Namespace("System.IO", Denied)
                .Namespace("System.Linq", Neutral, SetupSystemLinq)
                .Namespace("System.Reflection", Denied)
                .Namespace("System.Runtime", Denied)
                .Namespace("System.Runtime.InteropServices", Denied)
                .Namespace("System.Runtime.CompilerServices", Neutral, SetupSystemRuntimeCompilerServices)
                .Namespace("System.Text", Neutral, SetupSystemText)
                .Namespace("System.Text.RegularExpressions", Neutral, SetupSystemTextRegularExpressions)
                .Namespace("System.Threading", Denied)

                .Namespace("Unbreakable", Denied)
                .Namespace("Unbreakable.Runtime", Denied);
        }

        private static void SetupSystem(NamespacePolicy system) {
            system
                .Type(nameof(Activator), Denied)
                .Type(nameof(AppContext), Denied)
                .Type(nameof(AppDomain), Denied)
                .Type(nameof(AppDomainManager), Denied)
                .Type(typeof(Array), Neutral,
                    t => t.Member(nameof(Array.Copy), Allowed)
                          .Member(nameof(Array.Empty), Allowed)
                          .Member(nameof(Array.GetLength), Allowed)
                          .Member(nameof(Array.GetValue), Allowed)
                          .Getter(nameof(Array.Rank), Allowed)
                          .Member(nameof(Array.Resize), Allowed, new CountArgumentRewriter("newSize"))
                          .Member(nameof(Array.SetValue), Allowed)
                )
                .Type(nameof(ArgumentException), Neutral, t => t.Constructor(Allowed))
                .Type(nameof(ArgumentNullException), Neutral, t => t.Constructor(Allowed))
                .Type(nameof(ArgumentOutOfRangeException), Neutral, t => t.Constructor(Allowed))
                .Type(nameof(AttributeUsageAttribute), Allowed)
                .Type(nameof(Attribute), Allowed,
                    t => t.Member(nameof(Attribute.GetCustomAttribute), Denied)
                          .Member(nameof(Attribute.GetCustomAttributes), Denied)
                          .Member(nameof(Attribute.IsDefined), Denied)
                )
                .Type(nameof(BitConverter), Allowed,
                    t => t.Member(nameof(BitConverter.GetBytes), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(BitConverter.ToString), Allowed, StringReturnRewriter.Default)
                )
                .Type(nameof(Console), Denied)
                .Type(nameof(Convert), Allowed,
                    t => t.Member(nameof(Convert.FromBase64String), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Convert.FromBase64CharArray), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Convert.ToBase64String), Allowed, StringReturnRewriter.Default)
                )
                .Type(nameof(DateTime), Allowed,
                    t => t.Member(nameof(DateTime.GetDateTimeFormats), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(nameof(DateTimeKind), Allowed)
                .Type(nameof(DateTimeOffset), Allowed)
                .Type(nameof(DBNull), Allowed)
                .Type(nameof(Decimal), Allowed,
                    t => t.Member(nameof(Decimal.GetBits), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(nameof(Delegate), Neutral,
                    t => t.Member(nameof(Delegate.Combine), Allowed)
                          .Member(nameof(Delegate.Remove), Allowed)
                )
                .Type(nameof(Enum), Allowed,
                    t => t.Member(nameof(Enum.GetNames), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Enum.GetValues), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(nameof(Environment), Neutral,
                    t => t.Getter(nameof(Environment.CurrentManagedThreadId), Allowed)
                          .Getter(nameof(Environment.NewLine), Allowed)
                )
                .Type(nameof(Exception), Neutral, 
                    t => t.Constructor(Allowed)
                          .Getter(nameof(Exception.Message), Allowed)
                )
                .Type(nameof(FlagsAttribute), Allowed)
                .Type(nameof(GC), Neutral, 
                    t => t.Member(nameof(GC.SuppressFinalize), Allowed)
                )
                .Type(nameof(Guid), Allowed,
                    t => t.Member(nameof(Guid.ToByteArray), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(nameof(IConvertible), Allowed)
                .Type(nameof(IDisposable), Allowed)
                .Type(nameof(IFormattable), Allowed)
                .Type(nameof(InvalidCastException), Neutral, t => t.Constructor(Allowed))
                .Type(nameof(InvalidOperationException), Neutral, t => t.Constructor(Allowed))
                .Type(nameof(LocalDataStoreSlot), Denied)
                .Type(nameof(Math), Allowed)
                .Type(nameof(NotSupportedException), Neutral, t => t.Constructor(Allowed))
                .Type(nameof(Nullable), Allowed)
                .Type(typeof(Nullable<>).Name, Allowed)
                .Type(nameof(Object), Allowed)
                .Type(nameof(ObsoleteAttribute), Allowed)
                .Type(nameof(OperatingSystem), Denied)
                .Type(nameof(Random), Allowed)
                .Type(nameof(String), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.Default)
                          .Member(nameof(string.ToCharArray), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(string.Split), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(string.Join), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(string.Concat), Allowed, CollectedEnumerableArgumentRewriter.Default)
                )
                .Type(nameof(TimeSpan), Allowed)
                .Type(nameof(TimeZoneInfo), Allowed,
                    t => t.Member(nameof(TimeZoneInfo.ClearCachedData), Denied)
                          .Member(nameof(TimeZoneInfo.GetAdjustmentRules), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(TimeZoneInfo.GetAmbiguousTimeOffsets), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(nameof(Tuple), Allowed)
                .Type("TupleExtensions", Allowed)
                .Type(nameof(Type), Neutral,
                    t => t.Member(nameof(Type.GetTypeFromHandle), Allowed)
                          .Member(nameof(Type.IsAssignableFrom), Allowed)
                          .Member(nameof(Type.Equals), Allowed)
                          .Getter(nameof(Type.FullName), Allowed)
                          .Getter(nameof(Type.Name), Allowed)
                          .Getter(nameof(Type.Namespace), Allowed)
                          .Member("op_Equality", Allowed)
                )
                .Type(nameof(TypeCode), Allowed)
                .Type(nameof(TypedReference), Denied)
                .Type(typeof(Version).Name, Allowed)
                .Type(typeof(void).Name, Allowed)
                .Type(nameof(Uri), Allowed,
                    t => t.Getter(nameof(Uri.Segments), Allowed, ArrayReturnRewriter.Default)
                );

            foreach (var type in PrimitiveTypes.List) {
                if (type == typeof(IntPtr) || type == typeof(UIntPtr))
                    continue;
                system.Type(type, Allowed);
            }

            foreach (var type in DelegateTypes) {
                system.Type(type, Neutral, t => t.Constructor(Allowed).Member("Invoke", Allowed));
            }

            foreach (var type in TupleTypes) {
                system.Type(type, Allowed);
            }

            foreach (var typeName in ValueTupleTypeNames) {
                system.Type(typeName, Allowed);
            }
        }

        private static void SetupSystemCollections(NamespacePolicy collections) {
            collections
                .Type(nameof(DictionaryEntry), Allowed)
                .Type(nameof(ICollection), Allowed)
                .Type(nameof(IComparer), Allowed)
                .Type(nameof(IDictionary), Allowed, SetupAdd)
                .Type(nameof(IEqualityComparer), Allowed)
                .Type(nameof(IDictionaryEnumerator), Allowed)
                .Type(nameof(IList), Allowed, t => t.Other(SetupAdd, SetupInsert))
                .Type(nameof(IEnumerable), Allowed)
                .Type(nameof(IEnumerator), Allowed);
        }

        private static void SetupSystemCollectionsGeneric(NamespacePolicy collections) {
            collections
                .Type(typeof(Comparer<>), Allowed)
                .Type(typeof(Dictionary<,>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Other(SetupAdd)
                )
                .Type(typeof(Dictionary<,>.Enumerator), Allowed)
                .Type(typeof(Dictionary<,>.KeyCollection), Allowed)
                .Type(typeof(Dictionary<,>.ValueCollection), Allowed)
                .Type(typeof(EqualityComparer<>), Allowed)
                .Type(typeof(HashSet<>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, CollectedEnumerableArgumentRewriter.Default)
                          .Other(SetupSetCommon)
                )
                .Type(typeof(HashSet<>.Enumerator), Allowed)
                .Type(typeof(ICollection<>), Allowed, SetupAdd)
                .Type(typeof(IComparer<>), Allowed)
                .Type(typeof(IDictionary<,>), Allowed, SetupAdd)
                .Type(typeof(IEnumerable<>), Allowed)
                .Type(typeof(IEnumerator<>), Allowed)
                .Type(typeof(IEqualityComparer<>), Allowed)
                .Type(typeof(IList<>), Allowed, SetupInsert)
                .Type(typeof(IReadOnlyCollection<>), Allowed)
                .Type(typeof(IReadOnlyDictionary<,>), Allowed)
                .Type(typeof(IReadOnlyList<>), Allowed)
                .Type(typeof(ISet<>), Allowed, t => t.Other(SetupSetCommon))
                .Type(typeof(KeyNotFoundException), Allowed)
                .Type(typeof(KeyValuePair<,>), Allowed)
                .Type(typeof(LinkedList<>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, CollectedEnumerableArgumentRewriter.Default)
                )
                .Type(typeof(LinkedList<>.Enumerator), Allowed)
                .Type(typeof(LinkedListNode<>), Allowed)
                .Type(typeof(List<>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(List<object>.AddRange), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(List<object>.InsertRange), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(List<object>.ToArray), Allowed, ArrayReturnRewriter.Default)
                          .Other(SetupAdd, SetupInsert)
                )
                .Type(typeof(List<>.Enumerator), Allowed)
                .Type(typeof(Queue<>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Queue<object>.Enqueue), Allowed, AddCallRewriter.Default)
                          .Member(nameof(Queue<object>.ToArray), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(typeof(Queue<>.Enumerator), Allowed)
                .Type(typeof(SortedDictionary<,>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Other(SetupAdd)
                )
                .Type(typeof(SortedDictionary<,>.Enumerator), Allowed)
                .Type(typeof(SortedList<,>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Other(SetupAdd)
                )
                .Type(typeof(SortedSet<>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, CollectedEnumerableArgumentRewriter.Default)
                          .Other(SetupSetCommon)
                )
                .Type(typeof(SortedSet<>.Enumerator), Allowed)
                .Type(typeof(Stack<>), Allowed,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Stack<object>.Push), Allowed, AddCallRewriter.Default)
                          .Member(nameof(Stack<object>.ToArray), Allowed, ArrayReturnRewriter.Default)
                )
                .Type(typeof(Stack<>.Enumerator), Allowed);
        }

        private static void SetupSetCommon(TypePolicy set) {
            set.Member(nameof(ISet<object>.UnionWith), Allowed, CollectedEnumerableArgumentRewriter.Default)
               .Member(nameof(ISet<object>.IntersectWith), Allowed)
               .Member(nameof(ISet<object>.ExceptWith), Allowed)
               .Member(nameof(ISet<object>.SymmetricExceptWith), Allowed)
               .Member(nameof(ISet<object>.IsSubsetOf), Allowed)
               .Member(nameof(ISet<object>.IsProperSubsetOf), Allowed)
               .Member(nameof(ISet<object>.IsSupersetOf), Allowed)
               .Member(nameof(ISet<object>.IsProperSupersetOf), Allowed)
               .Member(nameof(ISet<object>.Overlaps), Allowed)
               .Member(nameof(ISet<object>.SetEquals), Allowed)
               .Other(SetupAdd);
        }

        private static void SetupAdd(TypePolicy type) {
            type.Member("Add", Allowed, AddCallRewriter.Default);
        }

        private static void SetupInsert(TypePolicy type) {
            type.Member("Insert", Allowed, AddCallRewriter.Default);
        }

        private static void SetupSystemComponentModel(NamespacePolicy componentModel) {
            componentModel
                .Type(nameof(TypeConverter), Neutral,
                    t => t.Member(nameof(TypeConverter.CanConvertFrom), Allowed)
                          .Member(nameof(TypeConverter.CanConvertTo), Allowed)
                          .Member(nameof(TypeConverter.ConvertFrom), Allowed)
                          .Member(nameof(TypeConverter.ConvertFromInvariantString), Allowed)
                          .Member(nameof(TypeConverter.ConvertFromString), Allowed)
                          .Member(nameof(TypeConverter.ConvertTo), Allowed)
                          .Member(nameof(TypeConverter.ConvertToInvariantString), Allowed)
                          .Member(nameof(TypeConverter.ConvertToString), Allowed)
                )
                .Type(nameof(TypeDescriptor), Neutral,
                    t => t.Member(nameof(TypeDescriptor.GetConverter), Allowed)
                );
        }

        private static void SetupSystemDiagnostics(NamespacePolicy diagnostics) {
            diagnostics
                .Type(nameof(DebuggerDisplayAttribute), Allowed)
                .Type(nameof(DebuggerHiddenAttribute), Allowed)
                .Type(nameof(DebuggerNonUserCodeAttribute), Allowed)
                .Type(nameof(DebuggerStepThroughAttribute), Allowed)
                .Type(nameof(Process), Denied)
                .Type(nameof(Stopwatch), Allowed);
        }

        private static void SetupSystemGlobalization(NamespacePolicy globalization) {
            globalization
                .Type(nameof(CultureInfo), Neutral,
                    t => t.Getter(nameof(CultureInfo.InvariantCulture), Allowed)
                );
        }

        private static void SetupSystemLinq(NamespacePolicy linq) {
            linq
                .Type(nameof(Enumerable), Neutral,
                    t => t.Member(nameof(Enumerable.Aggregate), Allowed)
                          .Member(nameof(Enumerable.All), Allowed)
                          .Member(nameof(Enumerable.Any), Allowed)
                          .Member(nameof(Enumerable.AsEnumerable), Allowed)
                          .Member(nameof(Enumerable.Average), Allowed)
                          .Member(nameof(Enumerable.Cast), Allowed)
                          .Member(nameof(Enumerable.Concat), Allowed)
                          .Member(nameof(Enumerable.Contains), Allowed)
                          .Member(nameof(Enumerable.Count), Allowed)
                          .Member(nameof(Enumerable.DefaultIfEmpty), Allowed)
                          .Member(nameof(Enumerable.Distinct), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.ElementAt), Allowed)
                          .Member(nameof(Enumerable.ElementAtOrDefault), Allowed)
                          .Member(nameof(Enumerable.Empty), Allowed)
                          .Member(nameof(Enumerable.Except), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.First), Allowed)
                          .Member(nameof(Enumerable.FirstOrDefault), Allowed)
                          .Member(nameof(Enumerable.GroupBy), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.GroupJoin), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Intersect), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Join), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Last), Allowed)
                          .Member(nameof(Enumerable.LastOrDefault), Allowed)
                          .Member(nameof(Enumerable.LongCount), Allowed)
                          .Member(nameof(Enumerable.Max), Allowed)
                          .Member(nameof(Enumerable.Min), Allowed)
                          .Member(nameof(Enumerable.OfType), Allowed)
                          .Member(nameof(Enumerable.OrderBy), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.OrderByDescending), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Range), Allowed, CountArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Repeat), Allowed, CountArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Reverse), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Select), Allowed)
                          .Member(nameof(Enumerable.SelectMany), Allowed)
                          .Member(nameof(Enumerable.SequenceEqual), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Single), Allowed)
                          .Member(nameof(Enumerable.SingleOrDefault), Allowed)
                          .Member(nameof(Enumerable.Skip), Allowed)
                          .Member(nameof(Enumerable.SkipWhile), Allowed)
                          .Member(nameof(Enumerable.Sum), Allowed)
                          .Member(nameof(Enumerable.Take), Allowed)
                          .Member(nameof(Enumerable.TakeWhile), Allowed)
                          .Member(nameof(Enumerable.ThenBy), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.ThenByDescending), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.ToArray), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.ToDictionary), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.ToList), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.ToLookup), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Union), Allowed, CollectedEnumerableArgumentRewriter.Default)
                          .Member(nameof(Enumerable.Where), Allowed)
                          .Member(nameof(Enumerable.Zip), Allowed)
                )
                .Type(typeof(IGrouping<,>), Allowed);
        }

        private static void SetupSystemRuntimeCompilerServices(NamespacePolicy compilerServices) {
            compilerServices
                .Type(nameof(CompilerGeneratedAttribute), Allowed)
                .Type(nameof(ExtensionAttribute), Allowed)
                .Type(nameof(IteratorStateMachineAttribute), Allowed)
                .Type("IsReadOnlyAttribute", Allowed)
                .Type(nameof(RuntimeHelpers), Neutral, 
                    t => t.Member(nameof(RuntimeHelpers.InitializeArray), Allowed)
                );
        }

        private static void SetupSystemText(NamespacePolicy text) {
            text
                .Type(nameof(Encoding), Allowed,
                    t => t.Member(nameof(Encoding.RegisterProvider), Denied)
                          .Setter(nameof(Encoding.DecoderFallback), Denied)
                          .Setter(nameof(Encoding.EncoderFallback), Denied)
                          .Member(nameof(Encoding.Convert), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Encoding.GetEncodings), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Encoding.GetPreamble), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Encoding.GetBytes), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Encoding.GetChars), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Encoding.GetString), Allowed, StringReturnRewriter.Default)
                )
                .Type(nameof(StringBuilder), Neutral,
                    t => t.Constructor(Allowed, CountArgumentRewriter.ForCapacity)
                          .Member(nameof(StringBuilder.Append), Allowed, AddCallRewriter.Default, CountArgumentRewriter.Default, new CountArgumentRewriter("repeatCount"))
                          .Member(nameof(StringBuilder.AppendFormat), Allowed, AddCallRewriter.Default)
                          .Member(nameof(StringBuilder.AppendLine), Allowed, AddCallRewriter.Default)
                          .Member(nameof(StringBuilder.Clear), Allowed)
                          .Member(nameof(StringBuilder.EnsureCapacity), Allowed, CountArgumentRewriter.ForCapacity)
                          .Getter(nameof(StringBuilder.Insert), Allowed, AddCallRewriter.Default, CountArgumentRewriter.Default)
                          .Getter(nameof(StringBuilder.Remove), Allowed)
                          .Getter(nameof(StringBuilder.Replace), Allowed)
                          .Getter(nameof(StringBuilder.ToString), Allowed)
                          .Getter(nameof(StringBuilder.Length), Allowed)
                          .Getter(nameof(StringBuilder.MaxCapacity), Allowed)
                );
        }

        private static void SetupSystemTextRegularExpressions(NamespacePolicy text) {
            text
                .Type(nameof(Regex), Neutral,
                    t => t.Constructor(Allowed, RegexDangerousMethodCallRewriter.Default)
                          .Getter(nameof(Regex.Options), Allowed)
                          .Getter(nameof(Regex.RightToLeft), Allowed)
                          .Member(nameof(Regex.IsMatch), Allowed, RegexDangerousMethodCallRewriter.Default)
                          .Member(nameof(Regex.Match), Allowed, RegexDangerousMethodCallRewriter.Default)
                          .Member(nameof(Regex.Matches), Allowed, RegexDangerousMethodCallRewriter.Default)
                          .Member(nameof(Regex.Replace), Allowed, RegexDangerousMethodCallRewriter.Default, StringReturnRewriter.Default)
                          .Member(nameof(Regex.Split), Allowed, RegexDangerousMethodCallRewriter.Default, ArrayReturnRewriter.Default)
                          .Member(nameof(Regex.GetGroupNames), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Regex.GetGroupNumbers), Allowed, ArrayReturnRewriter.Default)
                          .Member(nameof(Regex.GroupNameFromNumber), Allowed)
                          .Member(nameof(Regex.GroupNumberFromName), Allowed)
                          .Member(nameof(Regex.Escape), Allowed, StringReturnRewriter.Default)
                          .Member(nameof(Regex.Unescape), Allowed, StringReturnRewriter.Default)
                          .Member(nameof(Regex.ToString), Allowed, StringReturnRewriter.Default)
                )
                .Type(nameof(RegexOptions), Allowed, t => t.Member(nameof(RegexOptions.Compiled), Denied))
                .Type(nameof(Match), Allowed, t => t.Member(nameof(Match.Synchronized), Denied))
                .Type(nameof(MatchCollection), Allowed)
                .Type(nameof(MatchEvaluator), Neutral, t => t.Constructor(Allowed).Member("Invoke", Allowed))
                .Type(nameof(Capture), Allowed)
                .Type(nameof(CaptureCollection), Allowed)
                .Type(nameof(Group), Allowed, t => t.Member(nameof(Group.Synchronized), Denied))
                .Type(nameof(GroupCollection), Allowed);
        }

        private static TypePolicy CreateTypeRuleForCompilerGeneratedDelegate() {
            return new TypePolicy(Neutral)
                .Constructor(Allowed)
                .Member("Invoke", Allowed);
        }
    }
}
