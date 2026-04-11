using mooSQL.data.Common;
using System;



namespace mooSQL.data.model
{
	/// <summary>
	/// 二元表达式
	/// </summary>
	public class BinaryWord : ExpWordBase
	{
		/// <inheritdoc />
		public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
		
		/// <summary>使用 <see cref="DbDataType"/> 指定结果类型。</summary>
		public BinaryWord(DbDataType dbDataType, IExpWord expr1, string operation, IExpWord expr2, int precedence = PrecedenceLv.Unknown, Type type=null) : base(ClauseType.SqlBinaryExpression, type)
        {
			_expr1     = expr1     ?? throw new ArgumentNullException(nameof(expr1));
			Operation  = operation ?? throw new ArgumentNullException(nameof(operation));
			_expr2     = expr2     ?? throw new ArgumentNullException(nameof(expr2));
			Type       = dbDataType;
			Precedence = precedence;
		}

		/// <summary>使用 CLR 类型推断 <see cref="DbDataType"/>。</summary>
		public BinaryWord(Type systemType, IExpWord expr1, string operation, IExpWord expr2, int precedence = PrecedenceLv.Unknown)
			: this(new DbDataType(systemType), expr1, operation, expr2, precedence)
		{
		}

		private IExpWord _expr1;

		/// <summary>左操作数。</summary>
		public IExpWord Expr1
		{
			get => _expr1;
			set
			{
				_expr1    = value;
				_hashCode = null;
			}
		}

		/// <summary>运算符/关键字文本（如 <c>+</c>、<c>AND</c>）。</summary>
		public string         Operation  { get; }

		private IExpWord _expr2;

		/// <summary>右操作数。</summary>
		public IExpWord Expr2
		{
			get => _expr2;
			set
			{
				_expr2    = value;
				_hashCode = null;
			}
		}

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SqlBinaryExpression;

		/// <summary>结果 SQL 类型信息。</summary>
		public DbDataType Type { get; }


		/// <inheritdoc />
		public override int  Precedence { get; }

		int?                   _hashCode;
        /// <inheritdoc />
        public override Type SystemType => Type.SystemType;
        /// <inheritdoc />
        public override int GetHashCode()
		{
			// ReSharper disable NonReadonlyMemberInGetHashCode
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = Operation.GetHashCode();

			hashCode = unchecked(hashCode + (hashCode * 397) ^ Expr1.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ Expr2.GetHashCode());

			_hashCode = hashCode;
			return hashCode;
			// ReSharper restore NonReadonlyMemberInGetHashCode
		}

		#region ISqlExpression Members



		/// <inheritdoc />
		public override bool Equals(IExpWord? other, Func<IExpWord,IExpWord,bool> comparer)
		{
			if (this == other)
				return true;

			return
				other is BinaryWord expr  &&
				Operation  == expr.Operation       &&

				Expr1.Equals(expr.Expr1, comparer) &&
				Expr2.Equals(expr.Expr2, comparer) &&
				comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

		/// <inheritdoc />
		public  IElementWriter ToString(IElementWriter writer)
		{
			writer
				//.DebugAppendUniqueId(this)
				.AppendElement(Expr1)
				.Append(' ')
				.Append(Operation)
				.Append(' ')
				.AppendElement(Expr2);

			return writer;
		}

		#endregion

		/// <summary>解构为左右操作数与运算符。</summary>
		public void Deconstruct( out IExpWord expr1, out string operation, out IExpWord expr2)
		{

			expr1      = Expr1;
			operation  = Operation;
			expr2      = Expr2;
		}


	}
}
