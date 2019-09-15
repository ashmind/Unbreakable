using System;
using Unbreakable.Internal;

namespace Unbreakable {
    [Serializable]
    public struct RuntimeGuardToken {
        internal RuntimeGuardToken(Guid guid) {
            Guid = guid;
        }

        internal Guid Guid { get; }

        public IDisposable Scope(RuntimeGuardSettings? settings = null) {
            return new RuntimeGuardScope(this, settings ?? RuntimeGuardSettings.Default);
        }
    }
}
