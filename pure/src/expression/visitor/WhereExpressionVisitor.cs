
using mooSQL.data;
using mooSQL.data.call;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace mooSQL.linq
{
    /// <summary>
    /// where子句构建
    /// </summary>
    public class WhereExpressionVisitor: ConditionVisitor
    {
        /// <summary>
        /// where子句构建
        /// </summary>
        /// <param name="builder"></param>
        public WhereExpressionVisitor(FastCompileContext builder) : base(builder)
        {

            ValueVisitor = new ValueExpressionVisitor();
            MethodVisitor = new WhereMethodVisitor(builder, this);
        }


        /// <summary>
        /// 获取字段表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override string VisitToGotField(Expression node) {
            bool needNick = true;
            if (Context.RunType == LayerRunType.update && Builder.FromCount == 0)
            {
                needNick = false;
            }
            var fidv = new FieldVisitor(Context, needNick);
            var fie = fidv.Visit(node);
            if (fie is StringExpression str) {
                return str.Value;
            }
            return null;
        }
        /// <summary>
        /// 获取字段表达式
        /// </summary>
        /// <param name="node"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public override string VisitToGotField(Expression node,out MemberInfo member)
        {
            bool needNick = true;
            if (Context.RunType == LayerRunType.update && Builder.FromCount == 0)
            {
                needNick = false;
            }
            var fidv = new FieldVisitor(Context, needNick);
            var fie = fidv.Visit(node);
            if (fie is StringExpression str)
            {
                member = fidv.member;
                return str.Value;
            }
            member = null;
            return null;
        }

    }
}
