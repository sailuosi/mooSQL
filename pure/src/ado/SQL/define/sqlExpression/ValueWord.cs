using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	using Common;
	using Common.Internal;
    using mooSQL.utils;

    public class ValueWord : Clause, IExpWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitValueWord(this);
        }
        public ValueWord(Type systemType, object? value) : base(ClauseType.SqlValue, null)
        {
			_valueType = new DbDataType(value != null && value is not DBNull ? systemType.UnwrapNullable() : systemType);
			Value      = value;
		}

		public ValueWord(DbDataType valueType, object? value) : base(ClauseType.SqlValue, null)
        {
			_valueType    = valueType;
			Value         = value;
		}

		public ValueWord(object value) : base(ClauseType.SqlValue, null)
        {
			Value         = value ?? throw new ArgumentNullException(nameof(value), "Untyped null value");
			_valueType    = new DbDataType(value.GetType());
		}

		/// <summary>
		/// Provider specific value
		/// </summary>
		public object? Value { get; }

		DbDataType _valueType;

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

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

        #endregion

        #region ISqlExpression Members

        public int Precedence => PrecedenceLv.Primary;

		#endregion

		#region IEquatable<ISqlExpression> Members

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

		public bool CanBeNullable(ISQLNode nullability) => CanBeNull;

		public bool CanBeNull => Value == null;

		public bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			return ((IExpWord)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public ClauseType NodeType => ClauseType.SqlValue;

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

		public void Deconstruct(out object? value)
		{
			value = Value;
		}
	}
}
