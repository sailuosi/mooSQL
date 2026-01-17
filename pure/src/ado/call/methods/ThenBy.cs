using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ThenByCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitThenBy(this);
        }
        public ThenByCall() : base("ThenBy", null)
        {

        }
    }
}
