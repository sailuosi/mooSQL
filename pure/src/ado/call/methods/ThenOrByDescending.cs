using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ThenOrByDescendingCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitThenOrByDescending(this);
        }
        public ThenOrByDescendingCall() : base("ThenOrByDescending", null)
        {

        }
    }
}
