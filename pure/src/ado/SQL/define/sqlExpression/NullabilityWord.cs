using System;
using System.Linq;


namespace mooSQL.data.model
{
	public class NullabilityWord : ExpWordBase
    {
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitNullabilityExpression(this);
        }
        readonly bool           _isNullable;
		public   IExpWord SqlExpression { get; private set; }

		public NullabilityWord(IExpWord sqlExpression, bool isNullable, Type type = null) : base(ClauseType.SqlNullabilityExpression, type)
        {
			SqlExpression = sqlExpression;
			_isNullable   = isNullable;
		}

		public static IExpWord ApplyNullability(IExpWord sqlExpression, NullabilityContext nullability)
		{
			return sqlExpression switch
			{
				NullabilityWord => sqlExpression,
				SearchConditionWord       => sqlExpression,
				RowWord row     => new RowWord(row.Values.Select(v => ApplyNullability(v, nullability)).ToArray()),
				_ => new NullabilityWord(sqlExpression, nullability.CanBeNull(sqlExpression))
			};
		}

		public static IExpWord ApplyNullability(IExpWord sqlExpression, bool canBeNull)
		{
			switch (sqlExpression)
			{
				case SearchConditionWord:
					return sqlExpression;
				case RowWord row:
					return new RowWord(row.Values.Select(v => ApplyNullability(v, canBeNull)).ToArray());

				case NullabilityWord nullabilityExpression
						when nullabilityExpression.CanBeNull == canBeNull:
					return nullabilityExpression;
				case NullabilityWord nullabilityExpression:
					return new NullabilityWord(nullabilityExpression.SqlExpression, canBeNull);
					
				default:
					return new NullabilityWord(sqlExpression, canBeNull);
			}
		}

		public void Modify(IExpWord sqlExpression)
		{
			SqlExpression = sqlExpression;
		}

		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (NodeType != other.NodeType)
				return false;

			return SqlExpression.Equals(((NullabilityWord)other).SqlExpression, comparer);
		}

		public  bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public          bool             CanBeNull   => _isNullable;
		public override int              Precedence  => SqlExpression.Precedence;
		//public override Type?            SystemType  => SqlExpression.SystemType;
		public override ClauseType NodeType => ClauseType.SqlNullabilityExpression;
        public override Type? SystemType => SqlExpression.SystemType;
  //      public override int GetHashCode()
		//{
		//	// ReSharper disable once NonReadonlyMemberInGetHashCode
		//	return SqlExpression.GetHashCode();
		//}


	}
}
