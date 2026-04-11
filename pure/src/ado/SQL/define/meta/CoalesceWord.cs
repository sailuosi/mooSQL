using System;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// <c>COALESCE</c> / 空值合并表达式。
	/// </summary>
	public class CoalesceWord : ExpWordBase
	{

        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCoalesceExpression(this);
        }
		
		/// <summary>按顺序求第一个非空值。</summary>
		public CoalesceWord(Type type ,params IExpWord[] expressions) : base(ClauseType.SqlCoalesce, type)
        {
			Expressions = expressions;
		}

		/// <summary>候选表达式（从左到右）。</summary>
		public IExpWord[] Expressions { get; private set; }

		/// <inheritdoc />
		public override int              Precedence  => PrecedenceLv.LogicalDisjunction;
        /// <inheritdoc />
        public override Type? SystemType => Expressions[Expressions.Length-1].SystemType;
        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlCoalesce;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer.Append("$ISNULL$(");

			for (var index = 0; index < Expressions.Length; index++)
			{
				if (index > 0)
					writer.Append(", ");
				writer.AppendElement(Expressions[index]);
			}

			writer.Append(')');

			return writer;
		}

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not CoalesceWord otherCoalesceExpression)
				return false;

			if (Expressions.Length != otherCoalesceExpression.Expressions.Length)
				return false;

			for (var index = 0; index < Expressions.Length; index++)
			{
				if (!Expressions[index].Equals(otherCoalesceExpression.Expressions[index], comparer))
					return false;
			}

			return true;
		}



		/// <summary>替换候选列表。</summary>
		public void Modify(params IExpWord[] expressions)
		{
			Expressions = expressions;
		}
	}
}
