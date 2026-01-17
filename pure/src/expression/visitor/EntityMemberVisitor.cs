using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using System.Text;
using System.Threading.Tasks;
using mooSQL.data;
using mooSQL.data.linq;
using mooSQL.linq;

namespace mooSQL.linq
{
    /// <summary>
    /// 实体成员访问器，用于获取实体字段的数据库列名
    /// </summary>
    public abstract class EntityMemberVisitor :BaseExpressionSQLBuildVisitor
    {
        /// <summary>
        /// 构建结果是否需要源表别名前缀，例如：t.id,t.name
        /// </summary>
        public bool srcNick;
        public MemberInfo member { get; protected set; }
        public EntityColumn field { get; protected set; }

        public List<ParsedField> ParsedFields;

        public EntityMemberVisitor(FastCompileContext context) : base(context)
        {
            ParsedFields = new List<ParsedField>();
        }

        protected abstract EntityColumn GetFieldCol(MemberInfo prop,Expression expression);


        public string FindField(Expression expression)
        {
            var fie = Visit(expression);
            if (fie is StringExpression str)
            {
                return str.Value;
            }
            return null;
        }
        public EntityColumn FindFieldCol(Expression expression)
        {
            var fie = Visit(expression);
            if (this.field != null)
            {
                return field;
            }
            return null;
        }
        public override Expression Visit(Expression node)
        {
            if (node is StringExpression str)
            {
                return str;
            }
            return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var prop = node.Member;
            this.member = prop;
            var fie = this.GetFieldCol(prop,node);
            if (fie == null)
            {
                var baser = base.Visit(node.Expression);
                return baser;
            }
            this.field = fie;
            var t = "";
            if (!string.IsNullOrWhiteSpace(fie.DbColumnName))
            {
                t = fie.DbColumnName;
            }
            if (srcNick)
            {
                var nick = Context.CurrentLayer.getNick(prop.ReflectedType);
                if (nick != null)
                {
                    t = nick + "." + t;
                }
            }

            return new StringExpression(t);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Quote)
            {
                return Visit(node.Operand);
            }
            Visit(node.Operand);
            return node;
            //return base.VisitUnary(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Visit(node.Body);
            //return base.VisitLambda(node);
        }
    

    }
}
