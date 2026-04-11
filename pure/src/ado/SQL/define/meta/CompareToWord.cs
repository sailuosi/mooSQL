using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 比较表达式
	/// </summary>
	public class CompareToWord : ExpWordBase
	{
		
		/// <inheritdoc />
		public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCompareToExpression(this);
        }

		/// <summary>三值比较或元组比较占位节点。</summary>
		public CompareToWord(IExpWord expression1, IExpWord expression2,Type type=null) : base(ClauseType.CompareTo, type)
        {
			Expression1    = expression1;
			Expression2    = expression2;
		}

		/// <summary>左操作数。</summary>
		public IExpWord Expression1 { get; private set; }
		/// <summary>右操作数。</summary>
		public IExpWord Expression2 { get; private set; }

		/// <inheritdoc />
		public override int              Precedence  => PrecedenceLv.Unknown;
        /// <inheritdoc />
        public override Type? SystemType => typeof(int);
        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.CompareTo;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not CompareToWord compareTo)
				return false;

			return Expression1.Equals(compareTo.Expression1, comparer) && Expression2.Equals(compareTo.Expression2, comparer);
		}



		/// <summary>替换两侧表达式。</summary>
		public void Modify(IExpWord expression1, IExpWord expression2)
		{
			Expression1 = expression1;
			Expression2 = expression2;
		}
	}
}
