using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 条件表达式
	/// </summary>
	public class ConditionWord : ExpWordBase
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitConditionExpression(this);
        }
		public ConditionWord(IAffirmWord condition, IExpWord trueValue, IExpWord falseValue, Type type = null) : base(ClauseType.SqlCondition, type)
        {
			Condition  = condition;
			TrueValue  = trueValue;
			FalseValue = falseValue;
		}

		public IAffirmWord  Condition  { get; private set; }
		public IExpWord TrueValue  { get; private set; }
		public IExpWord FalseValue { get; private set; }

		public override int                    Precedence  => PrecedenceLv.Primary;
        public override Type? SystemType => TrueValue.SystemType;
        public override ClauseType       NodeType => ClauseType.SqlCondition;

		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				//.DebugAppendUniqueId(this)
				.Append("$IIF$(")
				.AppendElement(Condition)
				.Append(", ")
				.AppendElement(TrueValue)
				.Append(", ")
				.AppendElement(FalseValue)
				.Append(')');

			return writer;
		}

		public override bool Equals(IExpWord  other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			if (!(other is ConditionWord otherCondition))
				return false;

			return this.Condition.Equals(otherCondition.Condition, comparer) &&
			       this.TrueValue.Equals(otherCondition.TrueValue, comparer) &&
			       this.FalseValue.Equals(otherCondition.FalseValue, comparer);
		}



		public void Modify(IAffirmWord predicate, IExpWord trueValue, IExpWord falseValue)
		{
			Condition  = predicate;
			TrueValue  = trueValue;
			FalseValue = falseValue;
		}
	}
}
