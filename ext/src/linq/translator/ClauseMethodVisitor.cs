using mooSQL.data.call;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.translator
{
    /// <summary>
    /// 方法访问器，参数0为调用者，由此可以访问LINQ表达式的调用链
    /// </summary>
    internal partial class ClauseMethodVisitor:MethodVisitor
    {
        /// <summary>
        /// 合作的表达式访问器 搭档。当处理过程中需要临时移交给ExpressionVisitor进行处理时，交给它。
        /// </summary>
        public ExpressionVisitor Buddy {  get; set; }


        public override MethodCall VisitExpression(ExpressionCall method)
        {
            //由于本访问器，在表达式访问器的下层，遇到表达式时，直接返回，交给上层处理即可。
            return method;
        }




        #region 具体访问者

        public override MethodCall VisitAlias(AliasCall method)
        {
            var argu = method.Arguments[0];
            var next= Buddy.Visit(argu);
            return method.Expression(next);
        }

        public override MethodCall VisitAll(AllCall method)
        {

            return base.VisitAll(method);
        }
        #endregion

    }
}
