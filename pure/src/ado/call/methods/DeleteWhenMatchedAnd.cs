using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DeleteWhenMatchedAndCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDeleteWhenMatchedAnd(this);
        }
        public DeleteWhenMatchedAndCall() : base("DeleteWhenMatchedAnd", null)
        {

        }
    }
}
