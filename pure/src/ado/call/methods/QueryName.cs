using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class QueryNameCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitQueryName(this);
        }
        public QueryNameCall() : base("QueryName", null)
        {

        }
    }
}
