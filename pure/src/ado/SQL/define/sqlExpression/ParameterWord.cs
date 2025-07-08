using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace mooSQL.data.model
{
	using Common;

	public class ParameterWord :Clause, IExpWord
	{
		public ParameterWord(DbDataType type, string? name, object? value) : base(ClauseType.SqlParameter, null)
        {
			IsQueryParameter = true;
			Name             = name;
			Type             = type;
			Value            = value;

#if DEBUG
			_paramNumber = ++_paramCounter;
#endif
		}

#if DEBUG
		readonly int _paramNumber;
		static   int _paramCounter;
#endif

		// meh, nullable...
		public   string?    Name             { get; set; }
		public   DbDataType Type             { get; set; }
		public   bool       IsQueryParameter { get; set; }
        public int?       AccessorId       { get; set; }



		public object? Value     { get; }
		public bool    NeedsCast { get; set; }

		public object? CorrectParameterValue(object? rawValue)
		{
			var value = rawValue;

			var valueConverter = ValueConverter;
			return valueConverter == null ? value : valueConverter(value);
		}
        Type IExpWord.SystemType => Type.SystemType;
        #region Value Converter

        internal List<int>? TakeValues;

		private Func<object?, object?>? _valueConverter;
		public  Func<object?, object?>?  ValueConverter
		{
			get
			{
				if (_valueConverter == null && TakeValues != null)
					foreach (var take in TakeValues.ToArray())
						SetTakeConverter(take);

				return _valueConverter;
			}

			set => _valueConverter = value;
		}

		internal void SetTakeConverter(int take)
		{
			TakeValues ??= new List<int>();

			TakeValues.Add(take);

			SetTakeConverterInternal(take);
		}

		void SetTakeConverterInternal(int take)
		{
			var conv = _valueConverter;

			if (conv == null)
				_valueConverter = v => v == null ? null : ((int) v + take);
			else
				_valueConverter = v => v == null ? null : ((int) conv(v)! + take);
		}

		#endregion

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
			return ReferenceEquals(this, other);
		}

		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(ISQLNode nullability) => CanBeNull;

		public bool CanBeNull => DataTypeWord.TypeCanBeNull(Type.SystemType);

		public bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			return ((IExpWord)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.SqlParameter;

        IElementWriter ToString(IElementWriter writer)
		{
			if (NeedsCast)
				writer.Append("$Cast$(");

			if (Name?.StartsWith("@") == false)
				writer.Append('@');

			writer
				.Append(Name ?? "parameter");

#if DEBUG
			writer.Append('(').Append(_paramNumber).Append(')');
#endif
			if (Value != null)
				writer
					.Append('[')
					.Append(Value.ToString())
					.Append(']');

			if (NeedsCast)
				writer.Append(")");

			return writer;
		}

		#endregion
	}
}
