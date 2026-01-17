using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 比较表达式
	/// </summary>
	public class CompareToWord : ExpWordBase
	{
		
		public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCompareToExpression(this);
        }

		public CompareToWord(IExpWord expression1, IExpWord expression2,Type type=null) : base(ClauseType.CompareTo, type)
        {
			Expression1    = expression1;
			Expression2    = expression2;
		}

		public IExpWord Expression1 { get; private set; }
		public IExpWord Expression2 { get; private set; }

		public override int              Precedence  => PrecedenceLv.Unknown;
        public override Type? SystemType => typeof(int);
        public override ClauseType NodeType => ClauseType.CompareTo;

		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("$CompareTo$(")
				.AppendElement(Expression1)
				.Append(", ")
				.AppendElement(Expression2)
				.Append(')');

			return writer;
		}

		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not CompareToWord compareTo)
				return false;

			return Expression1.Equals(compareTo.Expression1, comparer) && Expression2.Equals(compareTo.Expression2, comparer);
		}



		public void Modify(IExpWord expression1, IExpWord expression2)
		{
			Expression1 = expression1;
			Expression2 = expression2;
		}
	}
}
