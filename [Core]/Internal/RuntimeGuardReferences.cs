using System;
using System.Reflection;
using Mono.Cecil;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    internal class RuntimeGuardReferences {
        private readonly ModuleDefinition _module;

        private static class MethodInfos {
            public static readonly MethodInfo GuardEnter = Get(nameof(RuntimeGuard.GuardEnter));
            public static readonly MethodInfo GuardJump = Get(nameof(RuntimeGuard.GuardJump));
            public static readonly MethodInfo GuardCountFlowThroughForIntPtr = Get(nameof(RuntimeGuard.GuardCountFlowThroughForIntPtr));
            public static readonly MethodInfo GuardCountFlowThroughForInt32 = Get(nameof(RuntimeGuard.GuardCountFlowThroughForInt32));
            public static readonly MethodInfo GuardCountFlowThroughForInt64 = Get(nameof(RuntimeGuard.GuardCountFlowThroughForInt64));

            private static MethodInfo Get(string name) => typeof(RuntimeGuard).GetMethod(name);
        }

        public RuntimeGuardReferences(FieldDefinition instanceField, ModuleDefinition module) {
            InstanceField = instanceField;
            GuardEnterMethod = module.Import(MethodInfos.GuardEnter);
            GuardJumpMethod = module.Import(MethodInfos.GuardJump);
            GuardCountIntPtrMethod = module.Import(MethodInfos.GuardCountFlowThroughForIntPtr);
            GuardCountInt32Method = module.Import(MethodInfos.GuardCountFlowThroughForInt32);
            GuardCountInt64Method = module.Import(MethodInfos.GuardCountFlowThroughForInt64);
            _module = module;
        }

        public FieldDefinition InstanceField { get; }
        public MethodReference GuardEnterMethod { get; }
        public MethodReference GuardJumpMethod { get; }
        public MethodReference GuardCountIntPtrMethod { get; }
        public MethodReference GuardCountInt32Method { get; }
        public MethodReference GuardCountInt64Method { get; }
    }
}
