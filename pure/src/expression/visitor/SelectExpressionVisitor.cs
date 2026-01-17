
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{

    public class SelectExpressionVisitor : BaseExpressionSQLBuildVisitor
    {
        public SelectExpressionVisitor(FastCompileContext builder) : base(builder)
        {
            valueVisitor = new ValueExpressionVisitor();
        }

        private ValueExpressionVisitor valueVisitor;

            
        public EntityContext EntityContext
        {
            get
            {
                return Builder.DBLive.client.EntityCash;
            }
        }
        private string asName;
        public void VisitFrom(Expression node, string asName)
        {
            this.asName = asName;
            this.Visit(node);
        }

        /// <summary>
        /// 属性访问
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            var prop = node.Member;
            var field= EntityContext.getFieldName(prop);
            if (!string.IsNullOrWhiteSpace(asName))
            {
                field = asName + "." + field;
            }
            Builder.select(field);
            return node;
        }
    }
}
