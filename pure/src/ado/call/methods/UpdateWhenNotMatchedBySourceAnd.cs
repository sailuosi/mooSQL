using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class UpdateWhenNotMatchedBySourceAndCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdateWhenNotMatchedBySourceAnd(this);
        }
        public UpdateWhenNotMatchedBySourceAndCall() : base("UpdateWhenNotMatchedBySourceAnd", null)
        {

        }
    }
}
