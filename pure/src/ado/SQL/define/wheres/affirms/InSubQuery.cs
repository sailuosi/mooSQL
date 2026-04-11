using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    /// <c>IN (SELECT …)</c> 子查询谓词（等价于 expression [ NOT ] IN 子查询或列表）。
    /// </summary>
    public class InSubQuery : BaseNotExpr
    {

        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmInSubQuery(this);
        }

        /// <summary>构造子查询 IN；<paramref name="doNotConvert"/> 禁止优化器改写子查询形状。</summary>
        public InSubQuery(IExpWord exp1, bool isNot, SelectQueryClause subQuery, bool doNotConvert)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            SubQuery = subQuery;
            DoNotConvert = doNotConvert;
        }

        /// <summary>是否禁止转换为半连接等等价形式。</summary>
        public bool DoNotConvert { get; }
        /// <summary>右侧子查询体。</summary>
        public SelectQueryClause SubQuery { get; private set; }

        /// <summary>替换左表达式与子查询。</summary>
        public void Modify(IExpWord exp1, SelectQueryClause subQuery)
        {
            Expr1 = exp1;
            SubQuery = subQuery;
        }

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is InSubQuery expr
                && SubQuery.Equals(expr.SubQuery, comparer)
                && base.Equals(other, comparer);
        }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability)
        {
            return true;
        }

        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new InSubQuery(Expr1, !IsNot, SubQuery, DoNotConvert);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.InSubQueryPredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            //writer.DebugAppendUniqueId(this);
            writer.AppendElement(Expr1);

            if (IsNot) writer.Append(" NOT");

            writer.Append(" IN");
            writer.AppendLine();
            using (writer.IndentScope())
            {
                writer.AppendLine('(');
                using (writer.IndentScope())
                {
                    writer.AppendElement(SubQuery);
                }
                writer.AppendLine();
                writer.Append(')');
            }

        }

        /// <summary>解构左表达式、NOT 标志与子查询。</summary>
        public void Deconstruct(out IExpWord exp1, out bool isNot, out SelectQueryClause subQuery)
        {
            exp1 = Expr1;
            isNot = IsNot;
            subQuery = SubQuery;
        }
    }

}
