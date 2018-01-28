namespace Unbreakable.Tools.Analyzer {
    public struct StackSize {
        private StackSize(long value, bool dependsOnGenericArguments) {
            Value = value;
            DependsOnGenericArguments = dependsOnGenericArguments;
        }

        public static StackSize DependingOnGenericArguments { get; } = new StackSize(0, true);

        public static implicit operator StackSize(long value) {
            return new StackSize(value, false);
        }

        public static StackSize operator +(StackSize left, StackSize right) {
            if (left.DependsOnGenericArguments || right.DependsOnGenericArguments)
                return DependingOnGenericArguments;
            return left.Value + right.Value;
        }

        public long Value { get; }
        public bool DependsOnGenericArguments { get; }

        public override string ToString() {
            return !DependsOnGenericArguments
                ? Value.ToString() + " bytes"
                : "(depends on generic arguments)";
        }
    }
}
