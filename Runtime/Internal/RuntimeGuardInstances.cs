using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Unbreakable.Runtime.Internal {
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RuntimeGuardInstances {
        private static readonly ConcurrentDictionary<Guid, RuntimeGuard> _instances = new ConcurrentDictionary<Guid, RuntimeGuard>();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static RuntimeGuard Get(string idString) {
            if (!_instances.TryGetValue(Guid.Parse(idString), out RuntimeGuard guard))
                throw new GuardException(GuardException.NoScopeMessage);
            return guard;
        }

        internal static void Start(Guid id, IRuntimeGuardSettings settings) {
            _instances.GetOrAdd(id, _ => new RuntimeGuard()).Start(settings);
        }

        internal static void Stop(Guid id) {
            if (!_instances.TryGetValue(id, out RuntimeGuard guard))
                return;
            guard.Stop();
        }
    }
}
