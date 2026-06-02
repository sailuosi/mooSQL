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
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitExpression(this);
        }
        /// <summary>
        /// 创建 Expression 方法调用节点。
        /// </summary>
        public ExpressionCall() : base("Expression", null)
        {

        }

        /// <summary>
        /// 属性 Value（Expression）。
        /// </summary>
        public Expression Value { get; set; }
    }
}