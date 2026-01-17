using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class GroupJoinCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitGroupJoin(this);
        }
        public GroupJoinCall() : base("GroupJoin", null)
        {

        }
    }
}
