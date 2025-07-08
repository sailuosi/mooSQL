using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InnerJoinCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInnerJoin(this);
        }
        public InnerJoinCall() : base("InnerJoin", null)
        {

        }
    }
}
