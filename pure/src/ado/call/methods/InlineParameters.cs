using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InlineParametersCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInlineParameters(this);
        }
        public InlineParametersCall() : base("InlineParameters", null)
        {

        }
    }
}
