using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace mooSQL.data.model
{
	/// <summary>
	/// SQL字段表达式，由别名和SQL值表达式组成
	/// </summary>
	public class ColumnWord : ExpWordBase,IField
	{
		/// <summary>SELECT 列表中的输出列（表达式 + 可选别名）。</summary>
		public ColumnWord(SelectQueryClause? parent, IExpWord expression, string? alias, Type type = null) : base(ClauseType.Column, type)
        {
			if (expression is SearchConditionWord)
			{

			}

			Parent      = parent;
			_expression = expression ?? throw new ArgumentNullException(nameof(expression));
			RawAlias    = alias;

#if DEBUG
			Number = Interlocked.Increment(ref _columnCounter);

			// useful for putting breakpoint when finding when SqlColumn was created
			if (Number == 0)
			{

			}
#endif
		}

		/// <summary>无别名构造。</summary>
		public ColumnWord(SelectQueryClause builder, IExpWord expression)
			: this(builder, expression, null)
		{
		}

#if DEBUG
		/// <summary>调试：列序号。</summary>
		public int Number { get; }

		static   int _columnCounter;
#endif

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IExpWord _expression;

        /// <summary>列值表达式。</summary>
        public IExpWord Expression
        {
            get => _expression;
            set
            {
                if (_expression == value)
                    return;
                if (ReferenceEquals(value, this))
                    throw new InvalidOperationException();
                _expression = value;
            }
        }
        /// <summary>同 <see cref="Expression"/>（<see cref="IField"/> 语义）。</summary>
        public IExpWord FieldValue
		{
			get => _expression;
			set
			{
				if (_expression == value)
					return;
				if (ReferenceEquals(value, this))
					throw new InvalidOperationException();
				_expression = value;
			}
		}

		/// <summary>所属 SELECT 查询体。</summary>
		public SelectQueryClause? Parent { get; set; }

        /// <summary>原始别名文本。</summary>
        public string? RawAlias   { get; set; }



		/// <inheritdoc />
		public string? Alias
		{
			get
			{
				return RawAlias;
			}
			set => RawAlias = value;
		}



		/// <inheritdoc />
		public override string ToString()
		{
#if OVERRIDETOSTRING
			var writer = new QueryElementTextWriter(NullabilityContext.GetContext(Parent));

			writer
				.Append('t')
				.Append(Parent?.SourceID ?? -1)
#if DEBUG
				.Append("[Id:").Append(Number).Append(']')
#endif
				.Append('.')
				.Append(Alias ?? "c")
				.Append(" => ")
				.AppendElement(Expression);

			var underlying = UnderlyingExpression();
			if (!ReferenceEquals(underlying, Expression))
			{
				writer
					.Append(" := ")
					.AppendElement(underlying);
			}

			if (CanBeNullable(writer.Nullability))
				writer.Append('?');

			return writer.ToString();

#else
			if (FieldValue is FieldWord or ColumnWord)
				return this.ToDebugString();

			return base.ToString()!;
#endif
		}

		#region ISqlExpression Members



		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			if (this == other)
				return true;

			if (!(other is ColumnWord otherColumn))
				return false;

			if (Parent != otherColumn.Parent)
				return false;

			if (Parent!.HasSetOperators)
				return false;

			return	comparer(this, other);
		}

		/// <inheritdoc />
		public override int   Precedence => PrecedenceLv.Primary;

        /// <inheritdoc />
        public override Type? SystemType => Expression .SystemType;
        #endregion

        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.Column;

        /// <summary>列所属的表节点（若有）。</summary>
        public ITableNode BelongTable {
			get; set;
		}
    }
}
