using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ThenOrByCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitThenOrBy(this);
        }
        public ThenOrByCall() : base("ThenOrBy", null)
        {

        }
    }
}
