using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// <c>SET</c> 子句中的单列赋值：<c>column = expression</c>。
	/// </summary>
	public class SetWord :Clause, ISQLNode
	{
		
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSetWord(this);
        }
		// 二者均可空，但至少一个不为空

		/// <summary>列或目标表达式与右侧表达式（右侧可为空表示特殊方言语义，由调用方保证合法）。</summary>
		public SetWord(IExpWord column, IExpWord? expression) : base(ClauseType.SetExpression, null)
        {
			Column    = column;
			Expression = expression;

			//ValidateColumnExpression(column, expression);
		}


		/// <summary>被赋值的列或目标表达式。</summary>
		public IExpWord  Column     { get; set; }
		/// <summary>右侧表达式。</summary>
		public IExpWord? Expression { get; set; }



		#region Overrides

#if OVERRIDETOSTRING

		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		/// <summary>调试输出文本。</summary>
		public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SetExpression;

		/// <inheritdoc />
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
