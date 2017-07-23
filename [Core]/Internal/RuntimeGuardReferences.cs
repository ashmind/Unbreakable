using System;
using System.Reflection;
using Mono.Cecil;
using Unbreakable.Runtime.Internal;

namespace Unbreakable {
    internal class RuntimeGuardReferences {
        private readonly ModuleDefinition _module;

        private static class MethodInfos {
            public static readonly MethodInfo GuardEnter = typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardEnter));
            public static readonly MethodInfo GuardEnterStaticConstructor = typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardEnterStaticConstructor));
            public static readonly MethodInfo GuardExitStaticConstructor = typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardExitStaticConstructor));
            public static readonly MethodInfo GuardJump = typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardJump));
            public static readonly MethodInfo GuardCount = typeof(RuntimeGuard).GetMethod(nameof(RuntimeGuard.GuardCount));
            public static readonly MethodInfo FlowThroughGuardCountIntPtr = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCountIntPtr));
            public static readonly MethodInfo FlowThroughGuardCountInt32 = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCountInt32));
            public static readonly MethodInfo FlowThroughGuardCountInt64 = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCountInt64));
            public static readonly MethodInfo FlowThroughGuardEnumerableIterated = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardEnumerableIterated));
            public static readonly MethodInfo FlowThroughGuardEnumerableCollected = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardEnumerableCollected));
        }

        public RuntimeGuardReferences(FieldDefinition instanceField, ModuleDefinition module) {
            InstanceField = instanceField;
            GuardEnterMethod = module.Import(MethodInfos.GuardEnter);
            GuardEnterStaticConstructorMethod = module.Import(MethodInfos.GuardEnterStaticConstructor);
            GuardExitStaticConstructorMethod = module.Import(MethodInfos.GuardExitStaticConstructor);
            GuardJumpMethod = module.Import(MethodInfos.GuardJump);
            GuardCountMethod = module.Import(MethodInfos.GuardCount);
            FlowThroughGuardCountIntPtrMethod = module.Import(MethodInfos.FlowThroughGuardCountIntPtr);
            FlowThroughGuardCountInt32Method = module.Import(MethodInfos.FlowThroughGuardCountInt32);
            FlowThroughGuardCountInt64Method = module.Import(MethodInfos.FlowThroughGuardCountInt64);
            FlowThroughGuardEnumerableIteratedMethod = module.Import(MethodInfos.FlowThroughGuardEnumerableIterated);
            FlowThroughGuardEnumerableCollectedMethod = module.Import(MethodInfos.FlowThroughGuardEnumerableCollected);
            _module = module;
        }

        public FieldDefinition InstanceField { get; }
        public FieldDefinition TestField_TEMP { get; }
        public MethodReference GuardEnterMethod { get; }
        public MethodReference GuardEnterStaticConstructorMethod { get; }
        public MethodReference GuardExitStaticConstructorMethod { get; }
        public MethodReference GuardJumpMethod { get; }
        public MethodReference GuardCountMethod { get; }
        public MethodReference FlowThroughGuardCountIntPtrMethod { get; }
        public MethodReference FlowThroughGuardCountInt32Method { get; }
        public MethodReference FlowThroughGuardCountInt64Method { get; }
        public MethodReference FlowThroughGuardEnumerableIteratedMethod { get; }
        public MethodReference FlowThroughGuardEnumerableCollectedMethod { get; }
    }
}
