using System;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	public class InlinedSqlWord : BaseInlinedWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInlinedSqlExpression(this);
        }
        public override ClauseType NodeType  => ClauseType.SqlInlinedExpression;

		public InlinedSqlWord(ParameterWord parameter, IExpWord inlinedValue,Type type=null) 
			: base(parameter, inlinedValue,ClauseType.SqlInlinedExpression,type)
		{
		}

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



		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not InlinedSqlWord otherInlined)
				return false;

			return Parameter.Equals(otherInlined.Parameter, comparer);
		}
	}
}
