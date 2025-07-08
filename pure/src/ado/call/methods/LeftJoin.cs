using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class LeftJoinCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitLeftJoin(this);
        }
        public LeftJoinCall() : base("LeftJoin", null)
        {

        }
    }
}
