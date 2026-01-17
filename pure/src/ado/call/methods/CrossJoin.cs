using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class CrossJoinCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitCrossJoin(this);
        }
        public CrossJoinCall() : base("CrossJoin", null)
        {

        }
    }
}
