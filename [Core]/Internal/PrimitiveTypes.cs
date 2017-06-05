using System;
using System.Collections.Generic;

namespace Unbreakable.Internal {
    internal static class PrimitiveTypes {
        public static IReadOnlyCollection<Type> List { get; } = new Type[] {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(char),
            typeof(float),
            typeof(double),
            typeof(bool)
        };
    }
}