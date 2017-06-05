using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Unbreakable.Internal {
    partial class ApiFilter {
        private static IDictionary<string, NamespaceRule> GetDefaultRules() {
            var systemRule = new NamespaceRule(ApiAccess.Neutral) {
                TypeRules = {
                    { nameof(Activator), new TypeRule(ApiAccess.Denied) },
                    { nameof(AppContext), new TypeRule(ApiAccess.Denied) },
                    { nameof(AppDomain), new TypeRule(ApiAccess.Denied) },
                    { nameof(AppDomainManager), new TypeRule(ApiAccess.Denied) },
                    { nameof(Console), new TypeRule(ApiAccess.Denied) },
                    { nameof(Environment), new TypeRule(ApiAccess.Denied) },
                    { nameof(GC), new TypeRule(ApiAccess.Denied) },
                    { nameof(LocalDataStoreSlot), new TypeRule(ApiAccess.Denied) },
                    { nameof(OperatingSystem), new TypeRule(ApiAccess.Denied) },
                    { nameof(TypedReference), new TypeRule(ApiAccess.Denied) },

                    { nameof(Object), new TypeRule(ApiAccess.Allowed) },
                    { nameof(String), new TypeRule(ApiAccess.Allowed) },
                    { "Void", new TypeRule(ApiAccess.Allowed) },
                    { nameof(Nullable), new TypeRule(ApiAccess.Allowed) },
                    { typeof(Nullable<>).Name, new TypeRule(ApiAccess.Allowed) },
                }
            };
            foreach (var type in PrimitiveTypes.List) {
                systemRule.TypeRules.Add(type.Name, new TypeRule(ApiAccess.Allowed));
            }

            return new Dictionary<string, NamespaceRule> {
                { nameof(System), systemRule },
                { "System.Collections.Generic", new NamespaceRule(ApiAccess.Allowed) },
                { "System.Linq", new NamespaceRule(ApiAccess.Allowed) {
                    TypeRules = {
                        { nameof(Enumerable), new TypeRule(ApiAccess.Allowed) {
                            MemberRules = { { nameof(Enumerable.Range), ApiAccess.Denied } }
                        } }
                    }
                } },
                { "System.Diagnostics", new NamespaceRule(ApiAccess.Denied) },
                { "System.IO", new NamespaceRule(ApiAccess.Denied) },
                { "System.Reflection", new NamespaceRule(ApiAccess.Denied) },
                { "System.Runtime", new NamespaceRule(ApiAccess.Denied) },
                { "System.Runtime.InteropServices", new NamespaceRule(ApiAccess.Denied) },
                { "System.Runtime.CompilerServices", new NamespaceRule(ApiAccess.Neutral) {
                    TypeRules = {
                        { nameof(CompilerGeneratedAttribute), new TypeRule(ApiAccess.Allowed) }
                    }
                } },
                { "System.Threading", new NamespaceRule(ApiAccess.Denied) },

                { "Unbreakable", new NamespaceRule(ApiAccess.Denied) },
                { "Unbreakable.Runtime", new NamespaceRule(ApiAccess.Denied) }
            };
        }
    }
}
