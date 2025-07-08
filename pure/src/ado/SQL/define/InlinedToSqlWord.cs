using System;

using mooSQL.data.model;


namespace mooSQL.linq.SqlQuery
{
	public class InlinedToSqlWord : BaseInlinedWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInlinedToSqlExpression(this);
        }
        public override ClauseType NodeType => ClauseType.SqlInlinedToSqlExpression;

		public InlinedToSqlWord(ParameterWord parameter, IExpWord inlinedValue,Type type=null) 
			: base(parameter, inlinedValue, ClauseType.SqlInlinedToSqlExpression, type)
		{
		}

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



		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not InlinedToSqlWord otherInlined)
				return false;

			return Parameter.Equals(otherInlined.Parameter, comparer);
		}

	}
}
