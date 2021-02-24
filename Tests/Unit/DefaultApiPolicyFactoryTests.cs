using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AshMind.Extensions;
using Xunit;
using Unbreakable.Internal;
using Unbreakable.Policy;
using Unbreakable.Policy.Internal;
using Unbreakable.Policy.Rewriters;

namespace Unbreakable.Tests.Unit {
    public class DefaultApiPolicyFactoryTests {
        #if LATEST
        [Fact]
        public void CreateSafeDefaultPolicy_DoesNotReferenceNonExistentTypesOrMembers_InLatestNet() {
            // Not in latest .NET
            var legacyTypeFullNames = new HashSet<string> {
                "System.AppDomainManager"
            };

            var policy = new DefaultApiPolicyFactory().CreateSafeDefaultPolicy();

            var missing = new HashSet<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var namespacePolicy in policy.Namespaces) {
                foreach (var typePolicy in namespacePolicy.Value.Types) {
                    var typeFullName = namespacePolicy.Key + "." + typePolicy.Key;
                    if (legacyTypeFullNames.Contains(typeFullName))
                        continue;

                    var type = assemblies
                        .Select(a => a.GetType(typeFullName, throwOnError: false))
                        .FirstOrDefault(t => t != null);
                    if (type == null) {
                        missing.Add("Type: " + typeFullName);
                        continue;
                    }

                    foreach (var memberPolicy in typePolicy.Value.Members) {
                        var memberName = memberPolicy.Key;
                        if (type.GetMember(memberName) == null)
                            missing.Add($"Member: {typeFullName}.{memberName}");
                    }
                }
            }

            Assert.Empty(missing);
        }
        #endif

        [Fact]
        public void CreateSafeDefaultPolicy_IncludesAddCallRewriter_ForMethodsNamedAddInsertEnqueueAndPush() {
            var methodNames = new HashSet<string> {
                "Add",
                "Insert",
                "Enqueue",
                "Push"
            };
            var excludedTypes = new HashSet<Type> {
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(decimal),
                typeof(string),
                typeof(TimeSpan)
            };
            AssertEachMatchingMethodHasRewriterOfType<AddCallRewriter>(
                m => methodNames.Contains(m.Name) && !excludedTypes.Contains(m.DeclaringType!)
            );
        }

        [Fact]
        public void CreateSafeDefaultPolicy_IncludesCountArgumentRewriter_ForMethodsWithParamerNamedCountOrCapacity() {
            var excluded = new HashSet<(Type, string)> {
                (typeof(string), nameof(string.Join)),
                (typeof(string), nameof(string.Split)),
                (typeof(Encoding), nameof(Encoding.GetByteCount)),
                (typeof(Encoding), nameof(Encoding.GetCharCount)),
                (typeof(Enumerable), nameof(Enumerable.Take)),
                (typeof(Enumerable), nameof(Enumerable.Skip)),
#if NETCORE
                (typeof(Enumerable), nameof(Enumerable.TakeLast)),
                (typeof(Dictionary<,>), nameof(Dictionary<object, object>.TrimExcess)),
#endif
                (typeof(Regex), nameof(Regex.Replace)),
                (typeof(Regex), nameof(Regex.Split))
            };
            AssertEachMatchingMethodHasRewriterOfType<CountArgumentRewriter>(
                m => m.GetParameters().Any(p => p.Name == "count" || p.Name == "capacity")
                  && !m.GetParameters().Any(p => p.Name != null && Regex.IsMatch(p.Name, "^(.+Index|index)$"))
                  && !excluded.Contains((m.DeclaringType!, m.Name))
            );
        }

        [Fact]
        public void CreateSafeDefaultPolicy_IncludesCountArgumentRewriter_WithCorrectArgumentName() {
            var methodGroupsWithCountRewriter = GetAllAllowedMethods()
                .Where(m => m.rule?.Rewriters.OfType<CountArgumentRewriter>().Any() ?? false)
                .GroupBy(m => m.rule)
                .Select(g => (
                    name: g.First().method.Name,
                    rewriters: g.Key.Rewriters.OfType<CountArgumentRewriter>().ToList(),
                    methods: g.Select(m => m.method).ToList()
                ));

            Assert.All(methodGroupsWithCountRewriter,
                g => Assert.All(g.rewriters,
                    r => Assert.Contains(r.CountParameterName,
                        g.methods.SelectMany(m => m.GetParameters().Select(p => p.Name)).Distinct()                      
                    )
                )
            );
        }

        [Fact]
        public void CreateSafeDefaultPolicy_IncludesDisposableReturnRewriter_ForMethodsReturningIDiposable() {
            AssertEachMatchingMethodHasRewriterOfType<DisposableReturnRewriter>(
                m => (m.IsConstructor ? m.DeclaringType! : ((MethodInfo)m).ReturnType).GetTypeInfo().IsAssignableTo<IDisposable>()
                  && m.Name != "GetEnumerator"
            );
        }

        [Fact]
        public void CreateSafeDefaultPolicy_IncludesArrayReturnRewriter_ForMethodsReturningArrays() {
            var excluded = new HashSet<(Type, string)> {
                (typeof(Array), nameof(Array.Empty)),
                (typeof(Enumerable), nameof(Enumerable.ToArray))
            };
            AssertEachMatchingMethodHasRewriterOfType<ArrayReturnRewriter>(
                b => b is MethodInfo m && m.ReturnType.IsArray
                  && !excluded.Contains((m.DeclaringType!, m.Name))
            );
        }

        [Fact]
        public void CreateSafeDefaultPolicy_DoesNotAllowStaticMembers_IfTheyLookDangerous() {
            var excluded = new HashSet<(Type, string)> {
                (typeof(decimal), nameof(decimal.Add))
            };
            AssertNoAllowedMethodsMatching(
                (m, _) => m.IsStatic
                       && Regex.IsMatch(m.Name, "^(?:set_|Register|Add|Set|Update|Clear)")
                       && !excluded.Contains((m.DeclaringType!, m.Name))
            );
        }

        private static void AssertEachMatchingMethodHasRewriterOfType<TApiMemberRewriter>(Func<MethodBase, bool> matcher)
            where TApiMemberRewriter : IMemberRewriter
        {
            AssertNoAllowedMethodsMatching(
                (method, rule) => matcher(method)
                               && !(rule?.Rewriters.OfType<TApiMemberRewriter>().Any() ?? false)
            );
        }

        private static void AssertNoAllowedMethodsMatching(Func<MethodBase, MemberPolicy?, bool> matcher) {
            var matched = new HashSet<string>();

            foreach (var (method, rule) in GetAllAllowedMethods()) {
                if (matcher(method, rule))
                    matched.Add(DescribeMethod(method));
            }

            Assert.Empty(matched);
        }

        private static IEnumerable<(MethodBase method, MemberPolicy? rule)> GetAllAllowedMethods() {
            var policy = new DefaultApiPolicyFactory().CreateSafeDefaultPolicy();

            var filter = new ApiFilter(policy);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in GetExportedTypesSafe(assembly)) {
                    foreach (var method in type.GetMethods()) {
                        if (type.Namespace == null)
                            continue;

                        var result = filter.Filter(type.Namespace, type.Name, ApiFilterTypeKind.External, method.Name);
                        if (result.Kind != ApiFilterResultKind.Allowed)
                            continue;

                        yield return (method, result.MemberRule);
                    }
                }
            }
        }

        private static Type[] GetExportedTypesSafe(Assembly assembly) {
            try {
                return assembly.GetExportedTypes();
            }
            catch (NotSupportedException) {
                return Type.EmptyTypes;
            }
            catch (FileNotFoundException) {
                return Type.EmptyTypes;
            }
        }

        private static string DescribeMethod(MethodBase method) {
            var builder = new StringBuilder();
            builder.Append(method.DeclaringType!.Name)
                   .Append(".")
                   .Append(method.Name)
                   .Append("(")
                   .AppendJoin(", ", method.GetParameters().Select(p => p.ParameterType.Name))
                   .Append(")");
            return builder.ToString();
        }

        private static Type FindType(string fullName) {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType(fullName))
                .Where(t => t != null)
                .FirstOrDefault() ?? throw new Exception($"Type {fullName} was not found.");
        }
    }
}
