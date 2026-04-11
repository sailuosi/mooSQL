using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 在表达式树中查找首个类型为 <typeparamref name="T"/> 的节点（常量或 Lambda）。
    /// </summary>
    /// <typeparam name="T">要匹配的表达式节点类型。</typeparam>
    public class ExpressionFindVisitor<T>:ExpressionVisitor
        where T : Expression
    {
        T target;
        /// <summary>
        /// 从根节点开始访问，返回找到的节点或默认值。
        /// </summary>
        public T Find(Expression node) { 
            if (node is T tar) { 
                return tar;
            }
            var r= Visit(node);
            return target;        
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node is T tar)
            {
                this.target = tar;
                return tar;
            }
            return base.VisitConstant(node);
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<R>(Expression<R> node)
        {
            if (node is T tar) {
                this.target = tar;
                return tar;
            }
            return base.VisitLambda(node);
        }
    }
}
