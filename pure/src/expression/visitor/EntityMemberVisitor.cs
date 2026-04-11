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
        /// <summary>
        /// 最近一次解析到的实体成员（属性/字段）元数据。
        /// </summary>
        public MemberInfo member { get; protected set; }
        /// <summary>
        /// 最近一次解析到的实体列映射信息。
        /// </summary>
        public EntityColumn field { get; protected set; }

        /// <summary>
        /// 在一次访问过程中累积解析出的字段列表（用于多列/复合场景）。
        /// </summary>
        public List<ParsedField> ParsedFields;

        /// <summary>
        /// 使用指定的编译上下文创建实体成员访问器。
        /// </summary>
        /// <param name="context">快速编译上下文。</param>
        public EntityMemberVisitor(FastCompileContext context) : base(context)
        {
            ParsedFields = new List<ParsedField>();
        }

        /// <summary>
        /// 根据成员与表达式上下文解析对应的实体列映射。
        /// </summary>
        /// <param name="prop">成员元数据。</param>
        /// <param name="expression">成员表达式（含实例表达式）。</param>
        /// <returns>实体列；无法映射时返回 null。</returns>
        protected abstract EntityColumn GetFieldCol(MemberInfo prop,Expression expression);


        /// <summary>
        /// 访问表达式并返回数据库列名（或带表别名前缀的列引用字符串）。
        /// </summary>
        /// <param name="expression">成员或相关表达式。</param>
        /// <returns>列名字符串；解析失败返回 null。</returns>
        public string FindField(Expression expression)
        {
            var fie = Visit(expression);
            if (fie is StringExpression str)
            {
                return str.Value;
            }
            return null;
        }
        /// <summary>
        /// 访问表达式并返回完整的 <see cref="EntityColumn"/> 映射信息。
        /// </summary>
        /// <param name="expression">成员或相关表达式。</param>
        /// <returns>实体列；解析失败返回 null。</returns>
        public EntityColumn FindFieldCol(Expression expression)
        {
            var fie = Visit(expression);
            if (this.field != null)
            {
                return field;
            }
            return null;
        }
        /// <summary>
        /// 访问表达式节点；若已是 <see cref="StringExpression"/> 则直接返回。
        /// </summary>
        /// <param name="node">表达式节点。</param>
        /// <returns>访问结果表达式。</returns>
        public override Expression Visit(Expression node)
        {
            if (node is StringExpression str)
            {
                return str;
            }
            return base.Visit(node);
        }

        /// <summary>
        /// 访问成员表达式：解析为数据库列名，并可按需加上当前层表别名前缀。
        /// </summary>
        /// <param name="node">成员表达式。</param>
        /// <returns>包装列名的 <see cref="StringExpression"/> 或基类处理结果。</returns>
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

        /// <summary>
        /// 访问一元表达式；对 <see cref="ExpressionType.Quote"/> 直接访问操作数。
        /// </summary>
        /// <param name="node">一元表达式。</param>
        /// <returns>处理后的表达式。</returns>
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

        /// <summary>
        /// 访问 Lambda：仅处理其 <see cref="LambdaExpression.Body"/>。
        /// </summary>
        /// <typeparam name="T">委托类型。</typeparam>
        /// <param name="node">Lambda 表达式。</param>
        /// <returns>主体访问结果。</returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Visit(node.Body);
            //return base.VisitLambda(node);
        }
    

    }
}
