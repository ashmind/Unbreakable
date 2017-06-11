using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AshMind.Extensions;
using Unbreakable.Internal;
using Unbreakable.Rules.Rewriters;
using Xunit;

namespace Unbreakable.Tests.Unit {
    public class SafeDefaultApiRulesTests {
        [Fact]
        public void Create_IncludesExplicitMemberRule_ForAnyMethodWithIEnumerableParameters() {
            var namespaceRules = SafeDefaultApiRules.Create();
            foreach (var namespaceRule in namespaceRules.Namespaces) {
                if (namespaceRule.Value.Access == ApiAccess.Denied)
                    continue;

                foreach (var typeRule in namespaceRule.Value.Types) {
                    if (typeRule.Value.Access != ApiAccess.Allowed)
                        continue;

                    var type = FindType(namespaceRule.Key + "." + typeRule.Key);
                    foreach (var method in type.GetMembers().OfType<MethodBase>()) {
                        if (!method.GetParameters().Any(p => p.ParameterType.IsGenericTypeDefinedAs(typeof(IEnumerable<>))))
                            continue;

                        var rule = typeRule.Value.Members.GetValueOrDefault(method.Name);
                        Assert.True(
                            rule.Rewriters.OfType<EnumerableArgumentRewriter>().Any(),
                            $"Method {method} on type {type} does not have an explicit {nameof(EnumerableArgumentRewriter)}."
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
                .First();
        }
    }
}
