using System;
using Unbreakable.Runtime;

namespace Unbreakable.Internal {
    internal class RuntimeGuardScope : IDisposable {
        private readonly Guid _guid;

        public RuntimeGuardScope(RuntimeGuardToken token) {
            _guid = token.Guid;
            RuntimeGuardInstances.Prepare(_guid);
        }

        public void Dispose() {
            RuntimeGuardInstances.Disable(_guid);
        }
    }
}
