using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class UpdateWhenMatchedAndCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdateWhenMatchedAnd(this);
        }
        public UpdateWhenMatchedAndCall() : base("UpdateWhenMatchedAnd", null)
        {

        }
    }
}
