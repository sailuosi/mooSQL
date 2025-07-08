using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace mooSQL.data.model
{
	public class ExpressionWord : ExpWordBase,IValueNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
			return visitor.VisitExpression(this);
        }
        public ExpressionWord(Type? systemType, string expr, int precedence, SqlFlags flags, ParametersNullabilityType nullabilityType, bool? canBeNull, params IExpWord[] parameters):base(ClauseType.SqlExpression,systemType)
		{
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));


			Expr            = expr;
			Precedence      = precedence;
			Parameters      = parameters;
			Flags           = flags;
			NullabilityType = nullabilityType;
			_canBeNull      = canBeNull;
		}

		public ExpressionWord(Type? systemType, string expr, int precedence, params IExpWord[] parameters)
			: this(systemType, expr, precedence, SqlFlags.IsPure, ParametersNullabilityType.Undefined, null, parameters)
		{
		}

		public ExpressionWord(string expr, int precedence, params IExpWord[] parameters)
			: this(null, expr, precedence, parameters)
		{
		}

		public ExpressionWord(Type? systemType, string expr, params IExpWord[] parameters)
			: this(systemType, expr, PrecedenceLv.Unknown, parameters)
		{
		}

		public ExpressionWord(string expr, params IExpWord[] parameters)
			: this(null, expr, PrecedenceLv.Unknown, parameters)
		{
		}


		public override int                       Precedence        { get; }
        public override Type? SystemType { get; }
        public          string                    Expr              { get; }
		public          IExpWord[]          Parameters        { get; }
		public          SqlFlags                  Flags             { get; }
		public          bool?                     CanBeNullNullable => _canBeNull;
		public          ParametersNullabilityType NullabilityType   { get; }

		public bool             IsAggregate      => (Flags & SqlFlags.IsAggregate)      != 0;
		public bool             IsPure           => (Flags & SqlFlags.IsPure)           != 0;
		public bool             IsPredicate      => (Flags & SqlFlags.IsPredicate)      != 0;
		public bool             IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;

		#region ISqlExpression Members



		bool? _canBeNull;
		public  bool   CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}

		public static Func<IExpWord,IExpWord,bool> DefaultComparer = (x, y) => true;

		int? _hashCode;

		//[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		//public override int GetHashCode()
		//{
		//	if (_hashCode != null)
		//		return _hashCode.Value;

		//	var hashCode = Expr.GetHashCode();



		//	for (var i = 0; i < Parameters.Length; i++)
		//		hashCode = unchecked(hashCode + (hashCode * 397) ^ Parameters[i].GetHashCode());

		//	_hashCode = hashCode;

		//	return hashCode;
		//}

		public override bool Equals(IExpWord? other, Func<IExpWord,IExpWord,bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (!(other is ExpressionWord expr) || Expr != expr.Expr || Parameters.Length != expr.Parameters.Length)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(expr.Parameters[i], comparer))
					return false;

			return comparer(this, expr);
		}

		#endregion

		#region IQueryElement Members

		public override ClauseType NodeType => ClauseType.SqlExpression;

		public  IElementWriter ToString(IElementWriter writer)
		{
			writer.DebugAppendUniqueId(this);




			
				writer.Append(Expr)
					.Append('{')
					//.Append(string.Join(", ", ss.Select(s => string.Format(CultureInfo.InvariantCulture, "{0}", s))))
					.Append('}');

			return writer;
		}

		#endregion

		public bool Equals(object? obj)
		{
			return Equals(obj, DefaultComparer);
		}

		#region Public Static Members

		public static bool NeedsEqual(ISQLNode ex)
		{
			switch (ex.NodeType)
			{
				case ClauseType.SqlParameter:
				case ClauseType.SqlField    :
				case ClauseType.SqlQuery    :
				case ClauseType.Column      : return true;
				case ClauseType.SqlExpression:
				{
					var expr = (ExpressionWord)ex;
					if (expr.IsPredicate)
						return false;
					//if (QueryHelper.IsTransitivePredicate(expr))
					//	return false;
					return true;
				}
				case ClauseType.SearchCondition :
					return false;
				case ClauseType.SqlFunction :

					var f = (FunctionWord)ex;

					return f.Name switch
					{
						"EXISTS" => false,
						_        => true,
					};
			}

			return false;
		}

		#endregion
	}
}
