using System;



namespace mooSQL.data.model
{
	/// <summary>
	/// SQL <c>CAST(expr AS type)</c> 表达式节点。
	/// </summary>
	public class CastWord : ExpWordBase
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCastExpression(this);
        }
		
		/// <summary>
		/// 构造类型转换表达式；<paramref name="fromType"/> 为源侧数据类型节点（可选）；<paramref name="isMandatory"/> 表示是否必须生成 CAST。
		/// </summary>
		public CastWord(IExpWord expression, DbDataType toType, DataTypeWord? fromType, bool isMandatory = false, Type type = null) : base(ClauseType.SqlCast, type)
        {
			Expression  = expression;
			ToType      = toType;
			FromType    = fromType;
			IsMandatory = isMandatory;
		}

		/// <summary>目标 SQL/CLR 类型。</summary>
		public DbDataType     ToType    { get; private set; }
		/// <summary>与 <see cref="ToType"/> 相同，便于与基类命名一致。</summary>
		public DbDataType     Type        => ToType;
		/// <summary>被转换的表达式。</summary>
		public IExpWord Expression  { get; private set; }
		/// <summary>可选的源数据类型节点（用于分析或方言）。</summary>
		public DataTypeWord?   FromType    { get; private set; }
		/// <summary>是否强制生成 CAST（即使类型已兼容）。</summary>
		public bool           IsMandatory { get; }

		/// <inheritdoc />
		public override int              Precedence  => PrecedenceLv.Primary;
		/// <inheritdoc />
        public override Type SystemType => ToType.SystemType;
		/// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlCast;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			if (!(other is CastWord otherCast))
				return false;

			return ToType.Equals(otherCast.ToType) && Expression.Equals(otherCast.Expression, comparer);
		}



		/// <summary>返回标记为必须 CAST 的副本（已为强制时返回自身）。</summary>
		public CastWord MakeMandatory()
		{
			if (IsMandatory)
				return this;
			return new CastWord(Expression, ToType, FromType, true);
		}

		/// <summary>替换被转换表达式（引用相等则返回自身）。</summary>
		public CastWord WithExpression(IExpWord expression)
		{
			if (ReferenceEquals(expression, Expression))
				return this;
			return new CastWord(expression, ToType, FromType, IsMandatory);
		}

		/// <summary>替换目标类型（相等则返回自身）。</summary>
		public CastWord WithToType(DbDataType toType)
		{
			if (toType == ToType)
				return this;
			return new CastWord(Expression, toType, FromType, IsMandatory);
		}

		/// <summary>就地更新目标类型、表达式与可选源类型。</summary>
		public void Modify(DbDataType toType, IExpWord expression, DataTypeWord? fromType)
		{
			ToType     = toType;
			Expression = expression;
			FromType   = fromType;
		}
	}

}
