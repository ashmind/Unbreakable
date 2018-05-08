using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unbreakable.Tests.Internal {
    // Having problems compiling Span<T> with Roslyn, so using this instead
    public ref struct SpanStub<T> {
        private readonly T[] _array;

        public SpanStub(T[] array) {
            _array = array;
        }

        public ref T this[int i] => ref _array[i];
    }
}
