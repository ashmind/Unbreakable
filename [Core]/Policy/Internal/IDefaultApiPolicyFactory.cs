using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Unbreakable.Policy.Internal {
    internal interface IDefaultApiPolicyFactory {
        [NotNull]
        ApiPolicy CreateSafeDefaultPolicy();
    }
}
