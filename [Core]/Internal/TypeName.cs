using System;
using Mono.Cecil;

namespace Unbreakable.Internal {
    internal readonly struct TypeName : IEquatable<TypeName> {
        public TypeName(Type type)
            : this(Argument.NotNull(nameof(type), type).Namespace, type?.Name)
        {
        }

        public TypeName(TypeReference type)
            : this(Argument.NotNull(nameof(type), type).Namespace, type?.Name) {
        }

        public TypeName(string @namespace, string name) {
            Namespace = @namespace;
            Name = Argument.NotNull(nameof(name), name);
        }

        public string Namespace { get; }
        public string Name { get; }

        public bool Matches(TypeReference reference) {
            return reference.Namespace == Namespace
                && reference.Name == Name;
        }

        public bool Equals(TypeName other) {
            return other.Namespace == Namespace
                && other.Name == Name;
        }

        public override bool Equals(object obj) {
            return (obj is TypeName name) && Equals(name);
        }

        public override int GetHashCode() {
            return (Namespace?.GetHashCode() ?? 0) ^ Name.GetHashCode();
        }
    }
}
