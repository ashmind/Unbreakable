namespace Unbreakable.Tools.Analyzer {
    public struct MethodFieldAccess {
        public bool Read { get; set; }
        public bool Write { get; set; }

        public override string ToString() {
            return Read
                ? (Write ? "Read/Write" : "Read")
                : (Write ? "Write" : "None");
        }
    }
}