using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unbreakable.Tests {
    public static class AssertEx {
        public static void DoesNotThrow(Action action) {
            action();
        }
    }
}
