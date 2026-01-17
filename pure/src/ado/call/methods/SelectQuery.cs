using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class SelectQueryCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSelectQuery(this);
        }
        public SelectQueryCall() : base("SelectQuery", null)
        {

        }
    }
}
