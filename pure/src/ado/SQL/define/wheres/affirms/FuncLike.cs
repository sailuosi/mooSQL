using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    /// <summary>
    ///  CONTAINS ( { column | * } , '< contains_search_condition >' )
    ///  FREETEXT ( { column | * } , 'freetext_string' )
    ///  expression { = | <> | != | > | >= | !> | < | <= | !< } { ALL | SOME | ANY } ( subquery )
    ///  EXISTS ( subquery )
    /// </summary>
    public class FuncLike : AffirmWord
    {

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmFuncLike(this);
        }

        public FuncLike(FunctionWord func)
            : base(func.Precedence)
        {
            Function = func;
        }

        public FunctionWord Function { get; private set; }

        public void Modify(FunctionWord function)
        {
            Function = function;
        }

        public override bool CanInvert(ISQLNode nullability) => false;
        public override IAffirmWord Invert(ISQLNode nullability) => throw new InvalidOperationException();

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is FuncLike expr
                && Function.Equals(expr.Function, comparer);
        }

        public override ClauseType NodeType => ClauseType.FuncLikePredicate;

        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Function);
        }

        public FuncLike Update(FunctionWord function)
        {
            if (ReferenceEquals(Function, function))
                return this;
            return new FuncLike(function);
        }
    }

}
