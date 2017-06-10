using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Unbreakable.Rules.Internal {
    internal interface IApiMemberRewriterInternal : IApiMemberRewriter {
        int Rewrite(Instruction instruction, ApiMemberRewriterContext context);
    }
}
