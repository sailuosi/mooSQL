using System;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// 聚合表达式
	/// </summary>
	public class CoalesceWord : ExpWordBase
	{

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCoalesceExpression(this);
        }
		
		public CoalesceWord(Type type ,params IExpWord[] expressions) : base(ClauseType.SqlCoalesce, type)
        {
			Expressions = expressions;
		}

		public IExpWord[] Expressions { get; private set; }

		public override int              Precedence  => PrecedenceLv.LogicalDisjunction;
        public override Type? SystemType => Expressions[Expressions.Length-1].SystemType;
        public override ClauseType NodeType => ClauseType.SqlCoalesce;

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



		public void Modify(params IExpWord[] expressions)
		{
			Expressions = expressions;
		}
	}
}
