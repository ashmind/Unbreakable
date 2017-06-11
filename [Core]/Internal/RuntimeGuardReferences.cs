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
            public static readonly MethodInfo GuardCountFlowThroughForIntPtr = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCountForIntPtr));
            public static readonly MethodInfo GuardCountFlowThroughForInt32 = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCountForInt32));
            public static readonly MethodInfo GuardCountFlowThroughForInt64 = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCountForInt64));
            public static readonly MethodInfo GuardIteratedEnumerable = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardIteratedEnumerable));
            public static readonly MethodInfo GuardCollectedEnumerable = typeof(RuntimeGuard.FlowThrough).GetMethod(nameof(RuntimeGuard.FlowThrough.GuardCollectedEnumerable));
        }

        public RuntimeGuardReferences(FieldDefinition instanceField, ModuleDefinition module) {
            InstanceField = instanceField;
            GuardEnterMethod = module.Import(MethodInfos.GuardEnter);
            GuardEnterStaticConstructorMethod = module.Import(MethodInfos.GuardEnterStaticConstructor);
            GuardExitStaticConstructorMethod = module.Import(MethodInfos.GuardExitStaticConstructor);
            GuardJumpMethod = module.Import(MethodInfos.GuardJump);
            GuardCountIntPtrMethod = module.Import(MethodInfos.GuardCountFlowThroughForIntPtr);
            GuardCountInt32Method = module.Import(MethodInfos.GuardCountFlowThroughForInt32);
            GuardCountInt64Method = module.Import(MethodInfos.GuardCountFlowThroughForInt64);
            GuardIteratedEnumerableMethod = module.Import(MethodInfos.GuardIteratedEnumerable);
            GuardCollectedEnumerableMethod = module.Import(MethodInfos.GuardCollectedEnumerable);
            _module = module;
        }

        public FieldDefinition InstanceField { get; }
        public MethodReference GuardEnterMethod { get; }
        public MethodReference GuardEnterStaticConstructorMethod { get; }
        public MethodReference GuardExitStaticConstructorMethod { get; }
        public MethodReference GuardJumpMethod { get; }
        public MethodReference GuardCountIntPtrMethod { get; }
        public MethodReference GuardCountInt32Method { get; }
        public MethodReference GuardCountInt64Method { get; }
        public MethodReference GuardIteratedEnumerableMethod { get; }
        public MethodReference GuardCollectedEnumerableMethod { get; }
    }
}
