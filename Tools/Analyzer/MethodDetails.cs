using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Unbreakable.Tools.Analyzer {
    public class MethodDetails {
        public MethodDetails(MethodBase method) {
            Method = method;
        }

        public MethodBase Method { get; }

        public bool IsTrusted { get; set; }
        public bool IsInternalCall { get; set; }
        public ISet<OpCode> UnknownOpCodes { get; } = new HashSet<OpCode>();
        public bool HasUnknownOpCodes => UnknownOpCodes.Count > 0;
        public StackSize StackPushSize { get; set; }
        public bool HasLoops { get; set; }
        public bool HasDirectRecursion { get; set; }

        public ISet<MethodBase> UntrustedCalls { get; } = new HashSet<MethodBase>();
        public IDictionary<FieldInfo, MethodFieldAccess> UntrustedFieldAccess { get; } = new Dictionary<FieldInfo, MethodFieldAccess>();

        public void AddCall(MethodDetails details) {
            if (details == this) {
                HasDirectRecursion = true;
                return;
            }

            if (!details.IsTrusted)
                UntrustedCalls.Add(details.Method);
            StackPushSize += details.StackPushSize;
            UnknownOpCodes.UnionWith(details.UnknownOpCodes);
        }

        public void AddUntrustedFieldAccess(FieldInfo field, MethodFieldAccess value) {
            if (!UntrustedFieldAccess.TryGetValue(field, out var access))
                access = new MethodFieldAccess();

            access.Read = access.Read || value.Read;
            access.Write = access.Write || value.Write;
            UntrustedFieldAccess[field] = access;
        }
    }
}