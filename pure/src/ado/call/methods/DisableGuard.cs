using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DisableGuardCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDisableGuard(this);
        }
        public DisableGuardCall() : base("DisableGuard", null)
        {

        }
    }
}
