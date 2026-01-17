using mooSQL.data.Common;
using System;



namespace mooSQL.data.model
{
	/// <summary>
	/// 二元表达式
	/// </summary>
	public class BinaryWord : ExpWordBase
	{
		public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
		
		public BinaryWord(DbDataType dbDataType, IExpWord expr1, string operation, IExpWord expr2, int precedence = PrecedenceLv.Unknown, Type type=null) : base(ClauseType.SqlBinaryExpression, type)
        {
			_expr1     = expr1     ?? throw new ArgumentNullException(nameof(expr1));
			Operation  = operation ?? throw new ArgumentNullException(nameof(operation));
			_expr2     = expr2     ?? throw new ArgumentNullException(nameof(expr2));
			Type       = dbDataType;
			Precedence = precedence;
		}

		public BinaryWord(Type systemType, IExpWord expr1, string operation, IExpWord expr2, int precedence = PrecedenceLv.Unknown)
			: this(new DbDataType(systemType), expr1, operation, expr2, precedence)
		{
		}

		private IExpWord _expr1;

		public IExpWord Expr1
		{
			get => _expr1;
			set
			{
				_expr1    = value;
				_hashCode = null;
			}
		}

		public string         Operation  { get; }

		private IExpWord _expr2;

		public IExpWord Expr2
		{
			get => _expr2;
			set
			{
				_expr2    = value;
				_hashCode = null;
			}
		}

		public override ClauseType NodeType => ClauseType.SqlBinaryExpression;

		public DbDataType Type { get; }


		public override int  Precedence { get; }

		int?                   _hashCode;
        public override Type SystemType => Type.SystemType;
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

		public void Deconstruct( out IExpWord expr1, out string operation, out IExpWord expr2)
		{

			expr1      = Expr1;
			operation  = Operation;
			expr2      = Expr2;
		}


	}
}
