using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ThenLoadCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitThenLoad(this);
        }
        public ThenLoadCall() : base("ThenLoad", null)
        {

        }
    }
}
