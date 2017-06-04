using System;
using System.Collections.Generic;

namespace Unbreakable.Internal {
    partial class ApiFilter {
        private static IDictionary<string, NamespaceRule> GetDefaultRules() {
            return new Dictionary<string, NamespaceRule> {
                { nameof(System), new NamespaceRule {
                    Access = ApiAccess.Neutral,
                    TypeRules = {
                        { nameof(Activator), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(AppContext), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(AppDomain), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(AppDomainManager), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(Console), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(Environment), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(GC), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(LocalDataStoreSlot), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(OperatingSystem), new TypeRule { Access = ApiAccess.Denied } },
                        { nameof(TypedReference), new TypeRule { Access = ApiAccess.Denied } },

                        { nameof(Byte), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(SByte), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(Boolean), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(Int16), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(UInt16), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(Int32), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(UInt32), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(Int64), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(UInt64), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(Object), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(String), new TypeRule { Access = ApiAccess.Allowed } },
                        { nameof(Type), new TypeRule { Access = ApiAccess.Allowed } },
                        { "Void", new TypeRule { Access = ApiAccess.Allowed } },
                    }
                } },
                { "System.Collections.Generic", new NamespaceRule { Access = ApiAccess.Allowed } },
                { "System.Linq", new NamespaceRule { Access = ApiAccess.Allowed } },
                { "System.Diagnostics", new NamespaceRule { Access = ApiAccess.Denied } },
                { "System.IO", new NamespaceRule { Access = ApiAccess.Denied } },
                { "System.Reflection", new NamespaceRule { Access = ApiAccess.Denied } },
                { "System.Runtime", new NamespaceRule { Access = ApiAccess.Denied } },
                { "System.Threading", new NamespaceRule { Access = ApiAccess.Denied } }
            };
        }
    }
}
