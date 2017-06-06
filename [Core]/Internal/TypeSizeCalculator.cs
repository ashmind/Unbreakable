using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;

namespace Unbreakable.Internal {
    internal static class TypeSizeCalculator {
        private static readonly IDictionary<string, int> PrimitiveTypeSizes = PrimitiveTypes.List.ToDictionary(
            t => t.FullName,
            t => Marshal.SizeOf(t)
        );

        public static int GetSize(TypeReference reference) {
            if (!reference.IsValueType)
                return IntPtr.Size;
            
            if (PrimitiveTypeSizes.TryGetValue(reference.FullName, out var size))
                return size;

            if (!(reference is TypeDefinition definition))
                definition = reference.Resolve();
            return GetSizeOfValueType(definition);
        }

        private static int GetSizeOfValueType(TypeDefinition definition) {
            // TODO: Generics
            // TODO: Explicit struct layout?
            var size = 0;
            foreach (var field in definition.Fields) {
                if (field.IsStatic)
                    continue;
                size += GetSize(field.FieldType);
            }
            return size;
        }
    }
}
