using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{
    /// <summary>
    /// 代表一个Expression ，用于将输出结果回转到Expression下，以便 Expression继续执行自己的 Visitor 体系
    /// </summary>
    public class ExpressionCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitExpression(this);
        }
        public ExpressionCall() : base("Expression", null)
        {

        }

        public Expression Value { get; set; }
    }
}
