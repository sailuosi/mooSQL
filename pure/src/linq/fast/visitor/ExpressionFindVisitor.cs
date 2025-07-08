using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class ExpressionFindVisitor<T>:ExpressionVisitor
        where T : Expression
    {
        T target;
        public T Find(Expression node) { 
            if (node is T tar) { 
                return tar;
            }
            var r= Visit(node);
            return target;        
        }



        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node is T tar)
            {
                this.target = tar;
                return tar;
            }
            return base.VisitConstant(node);
        }

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
