using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InsertWhenNotMatchedAndCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWhenNotMatchedAnd(this);
        }
        public InsertWhenNotMatchedAndCall() : base("InsertWhenNotMatchedAnd", null)
        {

        }
    }
}
