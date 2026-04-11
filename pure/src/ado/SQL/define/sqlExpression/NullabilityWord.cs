using System;
using System.Linq;


namespace mooSQL.data.model
{
	/// <summary>
	/// 为子表达式附加可空语义包装，用于在分析/生成阶段保留或改写表达式的可空信息。
	/// </summary>
	public class NullabilityWord : ExpWordBase
    {
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitNullabilityExpression(this);
        }
        readonly bool           _isNullable;
		/// <summary>被包装的 SQL 表达式。</summary>
		public   IExpWord SqlExpression { get; private set; }

		/// <summary>包装给定表达式并指定其是否可为空；<paramref name="type"/> 用于基类节点类型。</summary>
		public NullabilityWord(IExpWord sqlExpression, bool isNullable, Type type = null) : base(ClauseType.SqlNullabilityExpression, type)
        {
			SqlExpression = sqlExpression;
			_isNullable   = isNullable;
		}

		/// <summary>按上下文推断可空性：条件、行构造等递归处理，其余包装为 <see cref="NullabilityWord"/>。</summary>
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

		/// <summary>使用显式布尔可空标志应用包装；若已为相同值的 <see cref="NullabilityWord"/> 则直接返回。</summary>
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

		/// <summary>替换内部被包装的表达式。</summary>
		public void Modify(IExpWord sqlExpression)
		{
			SqlExpression = sqlExpression;
		}

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (NodeType != other.NodeType)
				return false;

			return SqlExpression.Equals(((NullabilityWord)other).SqlExpression, comparer);
		}

		/// <inheritdoc />
		public  bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		/// <inheritdoc />
		public          bool             CanBeNull   => _isNullable;
		/// <inheritdoc />
		public override int              Precedence  => SqlExpression.Precedence;
		//public override Type?            SystemType  => SqlExpression.SystemType;
		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SqlNullabilityExpression;
		/// <inheritdoc />
        public override Type? SystemType => SqlExpression.SystemType;
  //      public override int GetHashCode()
		//{
		//	// ReSharper disable once NonReadonlyMemberInGetHashCode
		//	return SqlExpression.GetHashCode();
		//}


	}
}
