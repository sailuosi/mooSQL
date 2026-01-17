using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DeleteWhenNotMatchedBySourceAndCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDeleteWhenNotMatchedBySourceAnd(this);
        }
        public DeleteWhenNotMatchedBySourceAndCall() : base("DeleteWhenNotMatchedBySourceAnd", null)
        {

        }
    }
}
