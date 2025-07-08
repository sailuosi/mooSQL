using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class SelectDistinctCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSelectDistinct(this);
        }
        public SelectDistinctCall() : base("SelectDistinct", null)
        {

        }
    }
}
