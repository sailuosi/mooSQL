using System;



namespace mooSQL.data.model
{
	/// <summary>
	/// cast 表达式
	/// </summary>
	public class CastWord : ExpWordBase
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCastExpression(this);
        }
		
		public CastWord(IExpWord expression, DbDataType toType, DataTypeWord? fromType, bool isMandatory = false, Type type = null) : base(ClauseType.SqlCast, type)
        {
			Expression  = expression;
			ToType      = toType;
			FromType    = fromType;
			IsMandatory = isMandatory;
		}

		public DbDataType     ToType    { get; private set; }
		public DbDataType     Type        => ToType;
		public IExpWord Expression  { get; private set; }
		public DataTypeWord?   FromType    { get; private set; }
		public bool           IsMandatory { get; }

		public override int              Precedence  => PrecedenceLv.Primary;
        public override Type SystemType => ToType.SystemType;
        public override ClauseType NodeType => ClauseType.SqlCast;

		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("CAST(")
				.AppendElement(Expression)
				.Append(" AS ")
				.Append(ToType.ToString())
				.Append(")");

			return writer;
		}

		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			if (!(other is CastWord otherCast))
				return false;

			return ToType.Equals(otherCast.ToType) && Expression.Equals(otherCast.Expression, comparer);
		}



		public CastWord MakeMandatory()
		{
			if (IsMandatory)
				return this;
			return new CastWord(Expression, ToType, FromType, true);
		}

		public CastWord WithExpression(IExpWord expression)
		{
			if (ReferenceEquals(expression, Expression))
				return this;
			return new CastWord(expression, ToType, FromType, IsMandatory);
		}

		public CastWord WithToType(DbDataType toType)
		{
			if (toType == ToType)
				return this;
			return new CastWord(Expression, toType, FromType, IsMandatory);
		}

		public void Modify(DbDataType toType, IExpWord expression, DataTypeWord? fromType)
		{
			ToType     = toType;
			Expression = expression;
			FromType   = fromType;
		}
	}

}
