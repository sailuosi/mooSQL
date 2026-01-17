using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class TakeCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitTake(this);
        }
        public TakeCall() : base("Take", null)
        {

        }
    }
}
