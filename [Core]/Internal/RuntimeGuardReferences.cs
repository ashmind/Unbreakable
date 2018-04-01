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
            public static readonly MethodInfo FlowThroughGuardEnumerableCollected = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardEnumerableCollected));
            public static readonly MethodInfo FlowThroughCollectDisposable = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.CollectDisposable));
        }

        public RuntimeGuardReferences(FieldDefinition instanceField, ModuleDefinition module) {
            InstanceField = instanceField;
            GuardEnterMethod = module.ImportReference(MethodInfos.GuardEnter);
            GuardEnterStaticConstructorMethod = module.ImportReference(MethodInfos.GuardEnterStaticConstructor);
            GuardExitStaticConstructorMethod = module.ImportReference(MethodInfos.GuardExitStaticConstructor);
            GuardJumpMethod = module.ImportReference(MethodInfos.GuardJump);
            GuardCountMethod = module.ImportReference(MethodInfos.GuardCount);
            FlowThroughGuardCountIntPtrMethod = module.ImportReference(MethodInfos.FlowThroughGuardCountIntPtr);
            FlowThroughGuardCountInt32Method = module.ImportReference(MethodInfos.FlowThroughGuardCountInt32);
            FlowThroughGuardCountInt64Method = module.ImportReference(MethodInfos.FlowThroughGuardCountInt64);
            FlowThroughGuardEnumerableCollectedMethod = module.ImportReference(MethodInfos.FlowThroughGuardEnumerableCollected);
            FlowThroughCollectDisposableMethod = module.ImportReference(MethodInfos.FlowThroughCollectDisposable);
            _module = module;
        }

        public FieldDefinition InstanceField { get; }
        public MethodReference GuardEnterMethod { get; }
        public MethodReference GuardEnterStaticConstructorMethod { get; }
        public MethodReference GuardExitStaticConstructorMethod { get; }
        public MethodReference GuardJumpMethod { get; }
        public MethodReference GuardCountMethod { get; }
        public MethodReference FlowThroughGuardCountIntPtrMethod { get; }
        public MethodReference FlowThroughGuardCountInt32Method { get; }
        public MethodReference FlowThroughGuardCountInt64Method { get; }
        public MethodReference FlowThroughGuardEnumerableCollectedMethod { get; }
        public MethodReference FlowThroughCollectDisposableMethod { get; }
    }
}
