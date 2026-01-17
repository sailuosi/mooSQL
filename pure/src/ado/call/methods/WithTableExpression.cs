using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class WithTableExpressionCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitWithTableExpression(this);
        }
        public WithTableExpressionCall() : base("WithTableExpression", null)
        {

        }
    }
}
