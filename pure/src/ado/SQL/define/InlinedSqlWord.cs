using System;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	/// <summary>
	/// 内联 SQL 表达式：参数值可直接为另一 <see cref="IExpWord"/>。
	/// </summary>
	public class InlinedSqlWord : BaseInlinedWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInlinedSqlExpression(this);
        }
        /// <inheritdoc />
        public override ClauseType NodeType  => ClauseType.SqlInlinedExpression;

		/// <summary>
		/// 构造内联 SQL 表达式节点。
		/// </summary>
		public InlinedSqlWord(ParameterWord parameter, IExpWord inlinedValue,Type type=null) 
			: base(parameter, inlinedValue,ClauseType.SqlInlinedExpression,type)
		{
		}

		/// <inheritdoc />
		public override IExpWord GetSqlExpression(EvaluateContext evaluationContext)
		{
			if (evaluationContext.ParameterValues == null)
				return InlinedValue;

			if (evaluationContext.ParameterValues.TryGetValue(Parameter, out var value))
			{
				if (value.ProviderValue is IExpWord sqlExpression)
					return sqlExpression;
			}

			return InlinedValue;
		}



		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not InlinedSqlWord otherInlined)
				return false;

			return Parameter.Equals(otherInlined.Parameter, comparer);
		}
	}
}
