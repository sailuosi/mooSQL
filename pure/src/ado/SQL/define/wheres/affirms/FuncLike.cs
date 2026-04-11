using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    /// 以函数调用形式表示的谓词：如全文检索 CONTAINS/FREETEXT、与 ALL/SOME/ANY 子查询比较、EXISTS 等。
    /// </summary>
    public class FuncLike : AffirmWord
    {

        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmFuncLike(this);
        }

        /// <summary>以函数调用形式包装为谓词（如 EXISTS、ALL/ANY/SOME、全文检索函数等）。</summary>
        public FuncLike(FunctionWord func)
            : base(func.Precedence)
        {
            Function = func;
        }

        /// <summary>底层函数表达式。</summary>
        public FunctionWord Function { get; private set; }

        /// <summary>就地替换为新的函数表达式。</summary>
        public void Modify(FunctionWord function)
        {
            Function = function;
        }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => false;
        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability) => throw new InvalidOperationException();

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is FuncLike expr
                && Function.Equals(expr.Function, comparer);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.FuncLikePredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Function);
        }

        /// <summary>返回使用新函数表达式的新 <see cref="FuncLike"/> 实例（引用相等时返回自身）。</summary>
        public FuncLike Update(FunctionWord function)
        {
            if (ReferenceEquals(Function, function))
                return this;
            return new FuncLike(function);
        }
    }

}
