using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AshMind.Extensions;
using Unbreakable.Internal;
using Unbreakable.Rules;
using Unbreakable.Rules.Rewriters;
using Xunit;

namespace Unbreakable.Tests.Unit {
    public class SafeDefaultApiRulesTests {
        [Fact]
        public void Create_IncludesEnumerableArgumentRewriter_ForAnyMethodWithIEnumerableParameters() {
            AssertEachMatchingMethodHasRewriterOfType<EnumerableArgumentRewriter>(
                m => m.GetParameters().Any(p => p.ParameterType.IsGenericTypeDefinedAs(typeof(IEnumerable<>)))
            );
        }

        [Fact]
        public void Create_IncludesAddCallRewriter_ForMethodsNamedAddEnqueueAndPush() {
            var excludedTypes = new HashSet<Type> {
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(Decimal),
                typeof(TimeSpan)
            };
            AssertEachMatchingMethodHasRewriterOfType<AddCallRewriter>(
                m => (m.Name == "Add" || m.Name == "Enqueue" || m.Name == "Push")
                  && (!excludedTypes.Contains(m.DeclaringType))
            );
        }

        private static void AssertEachMatchingMethodHasRewriterOfType<TApiMemberRewriter>(Func<MethodBase, bool> matcher)
            where TApiMemberRewriter : IApiMemberRewriter
        {
            var namespaceRules = SafeDefaultApiRules.Create();
            foreach (var namespaceRule in namespaceRules.Namespaces) {
                if (namespaceRule.Value.Access == ApiAccess.Denied)
                    continue;

                foreach (var typeRule in namespaceRule.Value.Types) {
                    if (typeRule.Value.Access != ApiAccess.Allowed)
                        continue;

                    var type = FindType(namespaceRule.Key + "." + typeRule.Key);
                    foreach (var method in type.GetMembers().OfType<MethodBase>()) {
                        if (!matcher(method))
                            continue;

                        var rule = typeRule.Value.Members.GetValueOrDefault(method.Name);
                        Assert.True(
                            rule?.Rewriters.OfType<TApiMemberRewriter>().Any(),
                            $"Method {method} on type {type} does not have an explicit {typeof(TApiMemberRewriter).Name}."
                        );
                    }
                }
            }
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
