using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	using Common;
	using Common.Internal;
    using mooSQL.utils;

	/// <summary>
	/// 字面量常量值节点（字符串、数字、<c>NULL</c> 等），带推断或显式的 <see cref="DbDataType"/>。
	/// </summary>
    public class ValueWord : Clause, IExpWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitValueWord(this);
        }
        /// <summary>按 CLR 类型与运行时值构造；<paramref name="value"/> 为 <c>NULL</c>/<see cref="DBNull"/> 时按可空规则处理类型。</summary>
        public ValueWord(Type systemType, object? value) : base(ClauseType.SqlValue, null)
        {
			_valueType = new DbDataType(value != null && value is not DBNull ? systemType.UnwrapNullable() : systemType);
			Value      = value;
		}

		/// <summary>显式指定数据库类型与值。</summary>
		public ValueWord(DbDataType valueType, object? value) : base(ClauseType.SqlValue, null)
        {
			_valueType    = valueType;
			Value         = value;
		}

		/// <summary>由非空运行时对象推断 CLR 类型（不可用于未类型的纯 null）。</summary>
		public ValueWord(object value) : base(ClauseType.SqlValue, null)
        {
			Value         = value ?? throw new ArgumentNullException(nameof(value), "Untyped null value");
			_valueType    = new DbDataType(value.GetType());
		}

		/// <summary>字面量对应的运行时值（字符串常量、数值、<see langword="null"/> 表示 SQL NULL）。</summary>
		public object? Value { get; }

		DbDataType _valueType;

		/// <summary>该字面量的数据类型描述（可修改，会清除缓存的哈希）。</summary>
		public DbDataType ValueType
		{
			get => _valueType;
			set
			{
				if (_valueType == value)
					return;
				_valueType = value;
				_hashCode  = null;
			}
		}

        Type IExpWord.SystemType => ValueType.SystemType;

        #region Overrides

#if OVERRIDETOSTRING

		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

        #endregion

        #region ISqlExpression Members

		/// <inheritdoc />
        public int Precedence => PrecedenceLv.Primary;

		#endregion

		#region IEquatable<ISqlExpression> Members

		/// <inheritdoc />
		bool IEquatable<IExpWord>.Equals(IExpWord? other)
		{
			if (this == other)
				return true;

			return
				other is ValueWord value           &&
				ValueType.Equals(value.ValueType) &&
				(Value == null && value.Value == null || Value != null && Value.Equals(value.Value));
		}

		int? _hashCode;

		/// <inheritdoc />
		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = 17;

			hashCode = unchecked(hashCode + (hashCode * 397) ^ ValueType.GetHashCode());

			if (Value != null)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Value.GetHashCode());

			_hashCode = hashCode;
			return hashCode;
		}

		#endregion

		#region ISqlExpression Members

		/// <inheritdoc />
		public bool CanBeNullable(ISQLNode nullability) => CanBeNull;

		/// <inheritdoc />
		public bool CanBeNull => Value == null;

		/// <inheritdoc />
		public bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			return ((IExpWord)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		/// <summary>调试输出文本。</summary>
		public string DebugText => this.ToDebugString();
#endif

		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SqlValue;

		/// <inheritdoc />
		IElementWriter ToString(IElementWriter writer)
		{
			return
				Value == null ?
					writer.Append("NULL") :
				Value is string strVal ?
					writer
						.Append('\'')
						.Append(strVal.Replace("\'", "''"))
						.Append('\'')
				:
					writer.Append(Value.ToString());
		}

		#endregion

		/// <summary>解构为字面量值。</summary>
		public void Deconstruct(out object? value)
		{
			value = Value;
		}
	}
}
