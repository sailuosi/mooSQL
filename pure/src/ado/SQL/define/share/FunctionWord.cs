using System;
using System.Linq;



namespace mooSQL.data.model
{
	/// <summary>
	/// SQL 函数调用表达式（如 <c>COUNT(*)</c>、<c>EXISTS(子查询)</c>），包含名称、参数与可空性推断信息。
	/// </summary>
	public class FunctionWord : ExpWordBase
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFunctionWord(this);
        }
		/// <summary>使用数据库类型与函数名构造；聚合/纯函数采用默认（非聚合、纯函数），优先级为 <see cref="PrecedenceLv.Primary"/>。</summary>
        public FunctionWord(DbDataType dbDataType, string name, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, false, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, type, parameters)
		{
		}

		/// <summary>指定是否为聚合函数与是否为纯函数（无副作用），其余与上一重载相同。</summary>
		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, bool isPure, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, isAggregate, isPure, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, type, parameters)
		{
		}

		/// <summary>指定是否为聚合函数；<c>isPure</c> 默认为 <see langword="true"/>。</summary>
		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, isAggregate, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, type, parameters)
		{
		}

		/// <summary>同时指定运算符优先级（用于括号省略规则）。</summary>
		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, int precedence, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null,type, parameters)
		{
		}

		/// <summary>完整构造函数：数据库类型、可空性策略与可选显式可空覆盖。</summary>
		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, bool isPure, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull,Type type, params IExpWord[] parameters) : base(ClauseType.SqlFunction, type)
        {
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var p in parameters)
				if (p == null) throw new ArgumentNullException(nameof(parameters));

			Type            = dbDataType;
			Name            = name;
			Precedence      = precedence;
			NullabilityType = nullabilityType;
			_canBeNull      = canBeNull;
			FunctionFlags = (isAggregate ? SqlFlags.IsAggregate : SqlFlags.None) |
			                (isPure ? SqlFlags.IsPure : SqlFlags.None);
			Parameters      = parameters;
		}

		/// <summary>由 CLR 类型推导 <see cref="DbDataType"/> 的便捷重载。</summary>
		public FunctionWord(Type systemType, string name, params IExpWord[] parameters)
			: this(systemType, name, false, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		/// <summary>指定聚合/纯函数标志的 CLR 类型重载。</summary>
		public FunctionWord(Type systemType, string name, bool isAggregate, bool isPure, params IExpWord[] parameters)
			: this(systemType, name, isAggregate, isPure, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		/// <summary>仅指定是否为聚合函数；<c>isPure</c> 默认为 <see langword="true"/>。</summary>
		public FunctionWord(Type systemType, string name, bool isAggregate, params IExpWord[] parameters)
			: this(systemType, name, isAggregate, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		/// <summary>指定优先级的 CLR 类型重载。</summary>
		public FunctionWord(Type systemType, string name, bool isAggregate, int precedence, params IExpWord[] parameters)
			: this(systemType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		/// <summary>完整 CLR 类型重载，委托给 <see cref="FunctionWord(DbDataType, string, bool, bool, int, ParametersNullabilityType, bool?, Type, IExpWord[])"/>。</summary>
		public FunctionWord(Type systemType, string name, bool isAggregate, bool isPure, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull, params IExpWord[] parameters)
		: this(new DbDataType(systemType), name, isAggregate, isPure, precedence, nullabilityType, canBeNull, systemType, parameters)
		{
		}

		/// <summary>函数在 SQL 中的数据类型（映射/方言相关）。</summary>
		public          DbDataType                Type              { get; }

		/// <summary>函数名（如 COUNT、EXISTS）。</summary>
		public          string                    Name              { get; }
		/// <inheritdoc />
		public override int                       Precedence        { get; }
		/// <inheritdoc />
        public override Type SystemType => Type.SystemType;
		/// <summary>聚合/纯函数等标志位。</summary>
        public          SqlFlags                  FunctionFlags     { get; }
		/// <summary>是否为聚合函数。</summary>
		public          bool                      IsAggregate       => (FunctionFlags & SqlFlags.IsAggregate) != 0;
		/// <summary>是否为纯函数（无副作用，可参与某些优化）。</summary>
		public          bool                      IsPure            => (FunctionFlags & SqlFlags.IsPure)      != 0;
		/// <summary>实参表达式数组。</summary>
		public          IExpWord[]          Parameters        { get; }
		/// <summary>显式指定的可空状态（若为 <see langword="null"/> 则按 <see cref="NullabilityType"/> 推断）。</summary>
		public          bool?                     CanBeNullNullable => _canBeNull;
		/// <summary>根据参数推断可空性的策略。</summary>
		public          ParametersNullabilityType NullabilityType   { get; }

		/// <summary>为 <see langword="true"/> 时跳过某些优化改写（如 EXISTS 相关）。</summary>
		public bool DoNotOptimize { get; set; }

		/// <summary>构造 <c>COUNT(*)</c> 聚合（参数为字面 <c>*</c> 表达式）。</summary>
		public static FunctionWord CreateCount(Type type, ITableNode table)
		{
			return new FunctionWord(type, "COUNT", true, true, PrecedenceLv.Primary,
				ParametersNullabilityType.NotNullable, null, new ExpressionWord("*"));
		}

		/// <summary>子查询量词 <c>ALL</c>。</summary>
		public static FunctionWord CreateAll   (SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "ALL",    false, PrecedenceLv.Comparison, subQuery); }
		/// <summary>子查询量词 <c>SOME</c>。</summary>
		public static FunctionWord CreateSome  (SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "SOME",   false, PrecedenceLv.Comparison, subQuery); }
		/// <summary>子查询量词 <c>ANY</c>。</summary>
		public static FunctionWord CreateAny   (SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "ANY",    false, PrecedenceLv.Comparison, subQuery); }
		/// <summary><c>EXISTS(子查询)</c>。</summary>
		public static FunctionWord CreateExists(SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "EXISTS", false, PrecedenceLv.Comparison, subQuery); }


		/// <summary>复制当前函数调用并替换函数名（名称相同时返回自身）。</summary>
        public FunctionWord WithName(string name)
        {
            if (name == Name)
                return this;
            return new FunctionWord(SystemType, name, IsAggregate, IsPure, Precedence, NullabilityType, _canBeNull, Parameters);
        }

        #region ISqlExpression Members



        bool?       _canBeNull;
		/// <inheritdoc />
		public  bool   CanBeNull
		{
			get => _canBeNull ?? NullabilityType != ParametersNullabilityType.NotNullable;
			set => _canBeNull = value;
		}

		#endregion

		#region Equals Members

		int? _hashCode;



		/// <inheritdoc />
		public override bool Equals(IExpWord? other, Func<IExpWord,IExpWord,bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (!(other is FunctionWord func) || Name != func.Name || Parameters.Length != func.Parameters.Length )
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(func.Parameters[i], comparer))
					return false;

			return comparer(this, func);
		}

		#endregion

		#region IQueryElement Members

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SqlFunction;

		/// <inheritdoc />
		public  IElementWriter ToString(IElementWriter writer)
		{
			writer.DebugAppendUniqueId(this);

			writer
				.Append(Name)
				.Append('(');

			var indent = false;
			// Handling Exists
			if (Parameters.Length == 1 && Parameters[0] is SelectQueryClause)
			{
				writer.AppendLine();
				writer.Indent();
				indent = true;
			}

			for (var index = 0; index < Parameters.Length; index++)
			{
				var p = Parameters[index];
				//p.ToString(writer);
				if (index < Parameters.Length - 1)
					writer.Append(", ");
			}

			if (indent)
			{
				writer.AppendLine();
				//writer.UnIndent();
			}

			writer.Append(')');



			return writer;
		}

		#endregion



		/// <summary>解构为函数名。</summary>
		public void Deconstruct(out string name)
		{
			name = Name;
		}


	}
}
