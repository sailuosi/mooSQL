using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 字段赋值部分 a=1,
	/// </summary>
	public class SetWord :Clause, ISQLNode
	{
		
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSetWord(this);
        }
		// 二者均可空，但至少一个不为空

		public SetWord(IExpWord column, IExpWord? expression) : base(ClauseType.SetExpression, null)
        {
			Column    = column;
			Expression = expression;

			//ValidateColumnExpression(column, expression);
		}


		public IExpWord  Column     { get; set; }
		public IExpWord? Expression { get; set; }



		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.SetExpression;

		IElementWriter ToString(IElementWriter writer)
		{
			writer
				.AppendElement(Column)
				.Append(" = ")
				.AppendElement(Expression);

			return writer;
		}

		#endregion
	}
}
