using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>支持可选 <c>NOT</c> 或外层取反的谓词基类。</summary>
    public abstract class BaseNotExpr : Expr
    {
        /// <summary>由子类指定左操作数、是否取反与优先级。</summary>
        protected BaseNotExpr(IExpWord exp1, bool isNot, int precedence)
            : base(exp1, precedence)
        {
            IsNot = isNot;
        }

        /// <summary>是否为否定形式。</summary>
        public bool IsNot { get; }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => true;

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is BaseNotExpr expr
                && IsNot == expr.IsNot
                && base.Equals(other, comparer);
        }

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            if (IsNot) writer.Append("NOT (");
            base.WritePredicate(writer);
            if (IsNot) writer.Append(')');
        }
    }

}
