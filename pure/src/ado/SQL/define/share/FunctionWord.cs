using System;
using System.Linq;



namespace mooSQL.data.model
{
	/// <summary>
	/// SQL 函数表达式
	/// </summary>
	public class FunctionWord : ExpWordBase
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFunctionWord(this);
        }
        public FunctionWord(DbDataType dbDataType, string name, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, false, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, type, parameters)
		{
		}

		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, bool isPure, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, isAggregate, isPure, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, type, parameters)
		{
		}

		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, isAggregate, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, type, parameters)
		{
		}

		public FunctionWord(DbDataType dbDataType, string name, bool isAggregate, int precedence, Type type, params IExpWord[] parameters)
			: this(dbDataType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null,type, parameters)
		{
		}

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

		public FunctionWord(Type systemType, string name, params IExpWord[] parameters)
			: this(systemType, name, false, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public FunctionWord(Type systemType, string name, bool isAggregate, bool isPure, params IExpWord[] parameters)
			: this(systemType, name, isAggregate, isPure, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public FunctionWord(Type systemType, string name, bool isAggregate, params IExpWord[] parameters)
			: this(systemType, name, isAggregate, true, PrecedenceLv.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public FunctionWord(Type systemType, string name, bool isAggregate, int precedence, params IExpWord[] parameters)
			: this(systemType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public FunctionWord(Type systemType, string name, bool isAggregate, bool isPure, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull, params IExpWord[] parameters)
		: this(new DbDataType(systemType), name, isAggregate, isPure, precedence, nullabilityType, canBeNull, systemType, parameters)
		{
		}

		public          DbDataType                Type              { get; }

		public          string                    Name              { get; }
		public override int                       Precedence        { get; }
        public override Type SystemType => Type.SystemType;
        public          SqlFlags                  FunctionFlags     { get; }
		public          bool                      IsAggregate       => (FunctionFlags & SqlFlags.IsAggregate) != 0;
		public          bool                      IsPure            => (FunctionFlags & SqlFlags.IsPure)      != 0;
		public          IExpWord[]          Parameters        { get; }
		public          bool?                     CanBeNullNullable => _canBeNull;
		public          ParametersNullabilityType NullabilityType   { get; }

		public bool DoNotOptimize { get; set; }

		public static FunctionWord CreateCount(Type type, ITableNode table)
		{
			return new FunctionWord(type, "COUNT", true, true, PrecedenceLv.Primary,
				ParametersNullabilityType.NotNullable, null, new ExpressionWord("*"));
		}

		public static FunctionWord CreateAll   (SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "ALL",    false, PrecedenceLv.Comparison, subQuery); }
		public static FunctionWord CreateSome  (SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "SOME",   false, PrecedenceLv.Comparison, subQuery); }
		public static FunctionWord CreateAny   (SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "ANY",    false, PrecedenceLv.Comparison, subQuery); }
		public static FunctionWord CreateExists(SelectQueryClause subQuery) { return new FunctionWord(typeof(bool), "EXISTS", false, PrecedenceLv.Comparison, subQuery); }


        public FunctionWord WithName(string name)
        {
            if (name == Name)
                return this;
            return new FunctionWord(SystemType, name, IsAggregate, IsPure, Precedence, NullabilityType, _canBeNull, Parameters);
        }

        #region ISqlExpression Members



        bool?       _canBeNull;
		public  bool   CanBeNull
		{
			get => _canBeNull ?? NullabilityType != ParametersNullabilityType.NotNullable;
			set => _canBeNull = value;
		}

		#endregion

		#region Equals Members

		int? _hashCode;



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

		public override ClauseType NodeType => ClauseType.SqlFunction;

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



		public void Deconstruct(out string name)
		{
			name = Name;
		}


	}
}
