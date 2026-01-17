using System;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	public abstract class BaseInlinedWord : ExpWordBase
    {
		protected BaseInlinedWord(ParameterWord parameter, IExpWord inlinedValue,ClauseType clauseType, Type type = null) : base(clauseType, type)
        {
			Parameter    = parameter;
			InlinedValue = inlinedValue;
		}

		public abstract IExpWord GetSqlExpression(EvaluateContext evaluationContext);

		public ParameterWord Parameter    { get; private set; }
		public IExpWord InlinedValue { get; private set; }

		public override int   Precedence => InlinedValue.Precedence;

        public override Type? SystemType => InlinedValue.SystemType;
        public void Modify(ParameterWord parameter, ExpWordBase inlinedValue)
		{
			Parameter    = parameter;
			InlinedValue = inlinedValue;
		}
	}
}
