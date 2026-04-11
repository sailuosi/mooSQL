using System;

using mooSQL.data.model;


namespace mooSQL.linq.SqlQuery
{
	/// <summary>
	/// 内联转 SQL：参数值经委托转换为 <see cref="IExpWord"/>。
	/// </summary>
	public class InlinedToSqlWord : BaseInlinedWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInlinedToSqlExpression(this);
        }
        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlInlinedToSqlExpression;

		/// <summary>
		/// 构造内联转 SQL 节点。
		/// </summary>
		public InlinedToSqlWord(ParameterWord parameter, IExpWord inlinedValue,Type type=null) 
			: base(parameter, inlinedValue, ClauseType.SqlInlinedToSqlExpression, type)
		{
		}

		/// <inheritdoc />
		public override IExpWord GetSqlExpression(EvaluateContext evaluationContext)
		{
			if (evaluationContext.ParameterValues == null)
				return InlinedValue;

			if (evaluationContext.ParameterValues.TryGetValue(Parameter, out var value))
			{
				if (value.ProviderValue is Func<SQLParameterValue,IExpWord> converter)
					return converter(value);
			}

			return InlinedValue;
		}



		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not InlinedToSqlWord otherInlined)
				return false;

			return Parameter.Equals(otherInlined.Parameter, comparer);
		}

	}
}
