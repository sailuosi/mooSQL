using mooSQL.data.call;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.linq.visitor
{
    /// <summary>
    /// 查询读取访问器，用于处理查询表达式。
    /// </summary>
    internal class QueryReadVisitor:MethodVisitor
    {
        /// <summary>
        /// 合作的表达式访问器 搭档。当处理过程中需要临时移交给ExpressionVisitor进行处理时，交给它。
        /// </summary>
        public ExpressionVisitor Buddy { get; set; }



        public override MethodCall VisitAll(AllCall method)
        {
            var argu = method.Arguments[0];
            var next = Buddy.Visit(argu);

            if (method.Arguments.Count == 2) { 
                var para=method.Arguments[1];
                //条件的内容表达式，需要借用where条件的访问器来做
            }

            return method.Expression(next);
        }

    }
}
