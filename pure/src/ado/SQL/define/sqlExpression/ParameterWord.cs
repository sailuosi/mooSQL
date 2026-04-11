using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace mooSQL.data.model
{
	using Common;

	/// <summary>
	/// SQL 参数表达式节点（如 <c>@name</c>），包含参数类型、可选名称与绑定值。
	/// </summary>
	public class ParameterWord :Clause, IExpWord
	{
		/// <summary>构造查询参数节点。</summary>
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
		/// <summary>参数名（通常带 <c>@</c> 前缀；输出时可自动补全）。</summary>
		public   string?    Name             { get; set; }
		/// <summary>参数的 SQL/CLR 数据类型描述。</summary>
		public   DbDataType Type             { get; set; }
		/// <summary>是否为查询级参数（与内部常量等相区别）。</summary>
		public   bool       IsQueryParameter { get; set; }
		/// <summary>访问器/列索引等附加标识（方言或提供程序相关）。</summary>
        public int?       AccessorId       { get; set; }



		/// <summary>绑定的常量值（若已求值）。</summary>
		public object? Value     { get; }
		/// <summary>生成 SQL 时是否需要外层 CAST 包装。</summary>
		public bool    NeedsCast { get; set; }

		/// <summary>在写入命令前对原始值做修正（含 <see cref="ValueConverter"/> 链）。</summary>
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
		/// <summary>可选的值转换委托（例如 TAKE 偏移等）。</summary>
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
			return ReferenceEquals(this, other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		#endregion

		#region ISqlExpression Members

		/// <inheritdoc />
		public bool CanBeNullable(ISQLNode nullability) => CanBeNull;

		/// <inheritdoc />
		public bool CanBeNull => DataTypeWord.TypeCanBeNull(Type.SystemType);

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
		public ClauseType NodeType => ClauseType.SqlParameter;

		/// <inheritdoc />
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
