using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model.affirms
{

    // { expression { = | <> | != | > | >= | ! > | < | <= | !< } expression
    /// <summary>
    /// 比较操作，如大于等于小于
    /// </summary>
    public class ExprExpr : Expr
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAffirmExprExpr(this);
        }
        /// <summary>二元比较谓词。</summary>
        public ExprExpr(IExpWord exp1, Operator op, IExpWord exp2, bool? withNull)
            : base(exp1, PrecedenceLv.Comparison)
        {
            Operator = op;
            Expr2 = exp2;
            WithNull = withNull;
        }

        /// <summary>比较运算符。</summary>
        public new Operator Operator { get; }
        /// <summary>右操作数。</summary>
        public IExpWord Expr2 { get;  set; }

        /// <summary>三值逻辑扩展（可空比较语义）。</summary>
        public bool? WithNull { get; }

        /// <inheritdoc />
        public override bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            return other is ExprExpr expr
                && WithNull == expr.WithNull
                && Operator == expr.Operator
                && Expr2.Equals(expr.Expr2, comparer)
                && base.Equals(other, comparer);
        }

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.ExprExprPredicate;

        /// <inheritdoc />
        protected override void WritePredicate(IElementWriter writer)
        {
            //writer.DebugAppendUniqueId(this);
            writer.AppendElement(Expr1);
            var op = Operator switch
            {
                Operator.Equal => "=",
                Operator.NotEqual => "<>",
                Operator.Greater => ">",
                Operator.GreaterOrEqual => ">=",
                Operator.NotGreater => "!>",
                Operator.Less => "<",
                Operator.LessOrEqual => "<=",
                Operator.NotLess => "!<",
                Operator.Overlaps => "OVERLAPS",
                _ => throw new InvalidOperationException(),
            };
            writer.Append(' ').Append(op).Append(' ')
                .AppendElement(Expr2);
        }

        /// <summary>取反比较运算符（用于 <see cref="Invert"/>）。</summary>
        static Operator InvertOperator(Operator op)
        {
            switch (op)
            {
                case Operator.Equal: return Operator.NotEqual;
                case Operator.NotEqual: return Operator.Equal;
                case Operator.Greater: return Operator.LessOrEqual;
                case Operator.NotLess:
                case Operator.GreaterOrEqual: return Operator.Less;
                case Operator.Less: return Operator.GreaterOrEqual;
                case Operator.NotGreater:
                case Operator.LessOrEqual: return Operator.Greater;
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>交换左右操作数时对应的运算符（对称化）。</summary>
        public static Operator SwapOperator(Operator op)
        {
            switch (op)
            {
                case Operator.Equal: return Operator.Equal;
                case Operator.NotEqual: return Operator.NotEqual;
                case Operator.Greater: return Operator.Less;
                case Operator.NotLess: return Operator.NotGreater;
                case Operator.GreaterOrEqual: return Operator.LessOrEqual;
                case Operator.Less: return Operator.Greater;
                case Operator.NotGreater: return Operator.NotLess;
                case Operator.LessOrEqual: return Operator.GreaterOrEqual;
                case Operator.Overlaps: return Operator.Overlaps;
                default: throw new InvalidOperationException();
            }
        }

        /// <inheritdoc />
        public override bool CanInvert(ISQLNode nullability) => true;

        /// <inheritdoc />
        public override IAffirmWord Invert(ISQLNode nullability)
        {
            return new ExprExpr(Expr1, InvertOperator(Operator), Expr2, !WithNull);
        }

        //public ISqlPredicate Reduce(ISQLNode nullability, EvaluationContext context, bool insideNot)
        //{
        //	ISqlPredicate MakeWithoutNulls()
        //	{
        //		return new ExprExpr(Expr1, Operator, Expr2, null);
        //	}

        //	if (Operator == Operator.Equal || Operator == Operator.NotEqual)
        //	{
        //		if (Expr1.TryEvaluateExpression(context, out var value1))
        //		{
        //			if (value1 == null)
        //				return new IsNull(Expr2, Operator != Operator.Equal);

        //		} else if (Expr2.TryEvaluateExpression(context, out var value2))
        //		{
        //			if (value2 == null)
        //				return new IsNull(Expr1, Operator != Operator.Equal);
        //		}
        //	}

        //	if (WithNull == null || nullability.IsEmpty)
        //		return this;

        //	if (!nullability.CanBeNull(Expr1) && !nullability.CanBeNull(Expr2))
        //		return MakeWithoutNulls();

        //	if (WithNull.Value)
        //	{
        //		if (Operator == Operator.Greater || Operator == Operator.Less)
        //			return this;

        //		if (Operator == Operator.NotEqual)
        //		{
        //			var search = new SearchConditionWord(true)
        //				.Add(MakeWithoutNulls())
        //				.AddAnd(sc => sc
        //					.Add(new IsNull(Expr1, false))
        //					.Add(new IsNull(Expr2, true)))
        //				.AddAnd(sc => sc
        //					.Add(new IsNull(Expr1, true))
        //					.Add(new IsNull(Expr2, false))
        //				);

        //			return search;
        //		}
        //		else
        //		{
        //			var search = new SearchConditionWord(true)
        //				.Add(MakeWithoutNulls())
        //				.AddAnd(sc => sc
        //					.Add(new IsNull(Expr1, false))
        //					.Add(new IsNull(Expr2, false))
        //				);

        //			return search;
        //		}
        //	}
        //	else
        //	{
        //		if (Operator == Operator.Equal)
        //			return this;

        //		if (Operator == Operator.NotEqual)
        //		{
        //			var search = new SearchConditionWord(true)
        //				.Add(MakeWithoutNulls())
        //				.AddAnd(sc => sc
        //					.Add(new IsNull(Expr1, false))
        //					.Add(new IsNull(Expr2, true)))
        //				.AddAnd(sc => sc
        //					.Add(new IsNull(Expr1, true))
        //					.Add(new IsNull(Expr2, false)));

        //			return search;
        //		}
        //		else
        //		{
        //			if (insideNot)
        //				return this;

        //			var search = new SearchConditionWord(true)
        //				.Add(MakeWithoutNulls())
        //				.Add(new IsNull(Expr1, false))
        //				.Add(new IsNull(Expr2, false));

        //			return search;
        //		}
        //	}
        //}

        /// <summary>解构为左操作数、运算符、右操作数与可空语义标志。</summary>
        public void Deconstruct(out IExpWord expr1, out Operator @operator, out IExpWord expr2, out bool? withNull)
        {
            expr1 = Expr1;
            @operator = Operator;
            expr2 = Expr2;
            withNull = WithNull;
        }
    }

}
