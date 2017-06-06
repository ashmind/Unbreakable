using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unbreakable.Rules;

namespace Unbreakable.Internal {
    internal static class SafeDefaultsApiRules {
        public static ApiRules Create() {
            return new ApiRules()
                .Namespace(nameof(System), ApiAccess.Neutral, SetupSystem)
                .Namespace("System.Collections.Generic", ApiAccess.Allowed)
                .Namespace("System.Linq", ApiAccess.Allowed, n => n.Type(nameof(Enumerable), ApiAccess.Allowed, t => t.Member(nameof(Enumerable.Range), ApiAccess.Denied)))
                .Namespace("System.Diagnostics", ApiAccess.Denied)
                .Namespace("System.IO", ApiAccess.Denied)
                .Namespace("System.Reflection", ApiAccess.Denied)
                .Namespace("System.Runtime", ApiAccess.Denied)
                .Namespace("System.Runtime.InteropServices", ApiAccess.Denied)
                .Namespace("System.Runtime.CompilerServices", ApiAccess.Neutral, n => n.Type(nameof(CompilerGeneratedAttribute), ApiAccess.Allowed))
                .Namespace("System.Threading", ApiAccess.Denied)

                .Namespace("Unbreakable", ApiAccess.Denied)
                .Namespace("Unbreakable.Runtime", ApiAccess.Denied);
        }

        private static void SetupSystem(NamespaceApiRule system) {
            system
                .Type(nameof(Activator), ApiAccess.Denied)
                .Type(nameof(AppContext), ApiAccess.Denied)
                .Type(nameof(AppDomain), ApiAccess.Denied)
                .Type(nameof(AppDomainManager), ApiAccess.Denied)
                .Type(nameof(Console), ApiAccess.Denied)
                .Type(nameof(Environment), ApiAccess.Denied)
                .Type(nameof(GC), ApiAccess.Denied)
                .Type(nameof(LocalDataStoreSlot), ApiAccess.Denied)
                .Type(nameof(OperatingSystem), ApiAccess.Denied)
                .Type(nameof(TypedReference), ApiAccess.Denied)

                .Type(nameof(Object), ApiAccess.Allowed)
                .Type(nameof(String), ApiAccess.Allowed)
                .Type(typeof(void).Name, ApiAccess.Allowed)
                .Type(nameof(Nullable), ApiAccess.Allowed)
                .Type(typeof(Nullable<>).Name, ApiAccess.Allowed);

            foreach (var type in PrimitiveTypes.List) {
                if (type == typeof(IntPtr) || type == typeof(UIntPtr))
                    continue;
                system.Type(type.Name, ApiAccess.Allowed);
            }
        }
    }
}
