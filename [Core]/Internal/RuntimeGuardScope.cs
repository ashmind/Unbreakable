using System;
using Unbreakable.Runtime.Internal;

namespace Unbreakable.Internal {
    internal class RuntimeGuardScope : IDisposable {
        private readonly Guid _guid;

        public RuntimeGuardScope(RuntimeGuardToken token, RuntimeGuardSettings settings) {
            _guid = token.Guid;
            RuntimeGuardInstances.Start(_guid, settings);
        }

        public void Dispose() {
            RuntimeGuardInstances.Stop(_guid);
        }
    }
}
