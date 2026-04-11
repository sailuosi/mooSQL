using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// 由模板字符串与若干子表达式参数构成的 SQL 表达式节点（函数调用、运算符等）。
	/// </summary>
	public class ExpressionWord : ExpWordBase,IValueNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
			return visitor.VisitExpression(this);
        }
        /// <summary>构造表达式节点（完整指定标志与可空性）。</summary>
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

		/// <summary>使用默认纯净标志与未定义可空性构造。</summary>
		public ExpressionWord(Type? systemType, string expr, int precedence, params IExpWord[] parameters)
			: this(systemType, expr, precedence, SqlFlags.IsPure, ParametersNullabilityType.Undefined, null, parameters)
		{
		}

		/// <summary>不指定 CLR 类型的便捷构造。</summary>
		public ExpressionWord(string expr, int precedence, params IExpWord[] parameters)
			: this(null, expr, precedence, parameters)
		{
		}

		/// <summary>使用未知优先级构造。</summary>
		public ExpressionWord(Type? systemType, string expr, params IExpWord[] parameters)
			: this(systemType, expr, PrecedenceLv.Unknown, parameters)
		{
		}

		/// <summary>最简构造（无 CLR 类型、未知优先级）。</summary>
		public ExpressionWord(string expr, params IExpWord[] parameters)
			: this(null, expr, PrecedenceLv.Unknown, parameters)
		{
		}


		/// <inheritdoc />
		public override int                       Precedence        { get; }
        /// <inheritdoc />
        public override Type? SystemType { get; }
        /// <summary>表达式模板文本（含占位）。</summary>
        public          string                    Expr              { get; }
		/// <summary>子表达式参数。</summary>
		public          IExpWord[]          Parameters        { get; }
		/// <summary>聚合/谓词等语义标志。</summary>
		public          SqlFlags                  Flags             { get; }
		/// <summary>可空性的可空引用表示。</summary>
		public          bool?                     CanBeNullNullable => _canBeNull;
		/// <summary>参数可空性推断类别。</summary>
		public          ParametersNullabilityType NullabilityType   { get; }

		/// <summary>是否为聚合函数。</summary>
		public bool             IsAggregate      => (Flags & SqlFlags.IsAggregate)      != 0;
		/// <summary>是否为无副作用纯表达式。</summary>
		public bool             IsPure           => (Flags & SqlFlags.IsPure)           != 0;
		/// <summary>是否为谓词（WHERE/HAVING 语境）。</summary>
		public bool             IsPredicate      => (Flags & SqlFlags.IsPredicate)      != 0;
		/// <summary>是否为窗口函数。</summary>
		public bool             IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;

		#region ISqlExpression Members



		bool? _canBeNull;
		/// <summary>逻辑上是否可为 NULL（未显式设置时默认为 true）。</summary>
		public  bool   CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}

		/// <summary>默认相等比较委托（恒为 true，由调用方覆盖语义）。</summary>
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SqlExpression;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public bool Equals(object? obj)
		{
			return Equals(obj, DefaultComparer);
		}

		#region Public Static Members

		/// <summary>判断该节点在比较语义上是否需要显式等号（用于生成优化提示等）。</summary>
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
