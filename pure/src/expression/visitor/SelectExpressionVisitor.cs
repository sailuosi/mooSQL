
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{

    /// <summary>
    /// 将投影/成员访问表达式转换为 SQLBuilder 的 select 片段调用的访问器。
    /// </summary>
    public class SelectExpressionVisitor : BaseExpressionSQLBuildVisitor
    {
        /// <summary>
        /// 使用指定的编译上下文创建 SELECT 表达式访问器。
        /// </summary>
        /// <param name="builder">快速编译上下文。</param>
        public SelectExpressionVisitor(FastCompileContext builder) : base(builder)
        {
            valueVisitor = new ValueExpressionVisitor();
        }

        private ValueExpressionVisitor valueVisitor;

            
        /// <summary>
        /// 当前数据库客户端上的实体映射上下文（列名解析等）。
        /// </summary>
        public EntityContext EntityContext
        {
            get
            {
                return Builder.DBLive.client.EntityCash;
            }
        }
        private string asName;
        /// <summary>
        /// 在指定表/子查询别名前缀下访问 FROM 侧表达式（影响生成的列引用前缀）。
        /// </summary>
        /// <param name="node">FROM 表达式。</param>
        /// <param name="asName">表或子查询别名。</param>
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
