using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class TableFromExpressionCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitTableFromExpression(this);
        }
        public TableFromExpressionCall() : base("TableFromExpression", null)
        {

        }
    }
}
