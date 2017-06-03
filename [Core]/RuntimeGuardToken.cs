using System;
using Unbreakable.Internal;

namespace Unbreakable {
    public struct RuntimeGuardToken {
        internal RuntimeGuardToken(Guid guid) {
            Guid = guid;
        }

        internal Guid Guid { get; }

        public IDisposable Scope() {
            return new RuntimeGuardScope(this);
        }
    }
}
