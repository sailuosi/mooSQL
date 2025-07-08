using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    public class InList : BaseNotExpr
    {

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmInList(this);
        }

        public bool? WithNull { get; }

        public InList(IExpWord exp1, bool? withNull, bool isNot)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            WithNull = withNull;
        }

        public InList(IExpWord exp1, bool? withNull, bool isNot, IExpWord value)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            WithNull = withNull;
            Values.Add(value);
        }

        public InList(IExpWord exp1, bool? withNull, bool isNot, IEnumerable<IExpWord>? values)
            : base(exp1, isNot, PrecedenceLv.Comparison)
        {
            WithNull = withNull;
            if (values != null)
                Values.AddRange(values);
        }

        public List<IExpWord> Values { get; private set; } = new();

        public void Modify(IExpWord expr1)
        {
            Expr1 = expr1;
        }

        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            if (other is not InList expr
                || WithNull != expr.WithNull
                || Values.Count != expr.Values.Count
                || !base.Equals(other, comparer))
                return false;

            for (var i = 0; i < Values.Count; i++)
                if (!Values[i].Equals(expr.Values[i], comparer))
                    return false;

            return true;
        }

        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new InList(Expr1, !WithNull, !IsNot, Values);
        }

        public override ClauseType NodeType => ClauseType.InListPredicate;

        protected override void WritePredicate(IElementWriter writer)
        {
            writer.AppendElement(Expr1);

            if (IsNot) writer.Append(" NOT");
            writer.Append(" IN (");

            foreach (var value in Values)
            {
                writer
                    .AppendElement(value)
                    .Append(',');
            }

            if (Values.Count > 0)
                //writer.Length--;

                writer.Append(')');
        }
    }


}
