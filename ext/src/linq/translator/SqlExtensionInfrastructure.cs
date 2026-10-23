using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.clause;

namespace mooSQL.linq;

public partial class DbFunc
{
	/// <summary>
	/// ISqlExtension
	/// </summary>
	[Obsolete("Use SooFunctionExtension.ISqlExtension.")]
	public interface ISqlExtension : SooFunctionExtension.ISqlExtension
	{
	}
	/// <summary>
	/// Ext
	/// </summary>

	[Obsolete("Use SooFunc.Ext.")]
	public static SooFunctionExtension.ISqlExtension? Ext => SooFunc.Ext;
	/// <summary>
	/// IExtensionCallBuilder
	/// </summary>

	public interface IExtensionCallBuilder
	{
		/// <summary>
		/// Builds SQL for Build.
		/// </summary>
		/// <summary>
		/// Builds SQL for Build.
		/// </summary>
		void Build(ISqExtensionBuilder builder);
	}
	/// <summary>
	/// IQueryableContainer
	/// </summary>

	public interface IQueryableContainer
	{
		/// <summary>
		/// Gets or sets Query.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		IQueryable Query { get; }
	}
	/// <summary>
	/// ISqExtensionBuilder
	/// </summary>

	public interface ISqExtensionBuilder
	{
		/// <summary>
		/// Configuration
		/// </summary>
		/// <summary>
		/// Gets or sets Configuration.
		/// </summary>
		string?         Configuration    { get; }
		/// <summary>
		/// Builds SQL for BuilderValue.
		/// </summary>
		/// <summary>
		/// Gets or sets BuilderValue.
		/// </summary>
		object?         BuilderValue     { get; }
		/// <summary>
		/// Gets or sets DBLive.
		/// </summary>

		DBInstance DBLive { get; }
		/// <summary>
		/// Query
		/// </summary>
		/// <summary>
		/// Gets or sets Query.
		/// </summary>
		SelectQueryClause     Query            { get; }
		/// <summary>
		/// Member
		/// </summary>
		/// <summary>
		/// Gets or sets Member.
		/// </summary>
		MemberInfo      Member           { get; }
		/// <summary>
		/// Extension
		/// </summary>
		/// <summary>
		/// Gets or sets Extension.
		/// </summary>
		SqlExtension    Extension        { get; }
		/// <summary>
		/// ResultExpression
		/// </summary>
		/// <summary>
		/// Gets or sets ResultExpression.
		/// </summary>
		IExpWord? ResultExpression { get; set; }
		/// <summary>
		/// IsConvertible
		/// </summary>
		/// <summary>
		/// Gets or sets IsConvertible.
		/// </summary>
		bool            IsConvertible    { get; set; }
		/// <summary>
		/// Expression
		/// </summary>
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>
		string          Expression       { get; set; }
		/// <summary>
		/// Arguments
		/// </summary>
		/// <summary>
		/// Gets or sets Arguments.
		/// </summary>
		Expression[]    Arguments        { get; }
		/// <summary>
		/// Gets or sets IsNullable.
		/// </summary>

		IsNullableType  IsNullable       { get; }
		/// <summary>
		/// CanBeNull
		/// </summary>
		/// <summary>
		/// Gets or sets CanBeNull.
		/// </summary>
		bool?           CanBeNull        { get; }
		/// <summary>
		/// Gets GetValue&lt;T&gt;.
		/// </summary>

		T      GetValue<T>   (int    index);
		/// <summary>
		/// Gets GetValue&lt;T&gt;.
		/// </summary>
		/// <summary>
		/// Gets GetValue&lt;T&gt;.
		/// </summary>
		T      GetValue<T>   (string argName);
		/// <summary>
		/// Gets GetObjectValue.
		/// </summary>
		/// <summary>
		/// Gets GetObjectValue.
		/// </summary>
		object GetObjectValue(int    index);
		/// <summary>
		/// Gets GetObjectValue.
		/// </summary>
		/// <summary>
		/// Gets GetObjectValue.
		/// </summary>
		object GetObjectValue(string argName);
		/// <summary>
		/// Gets GetExpression.
		/// </summary>

		IExpWord? GetExpression(int    index,   bool unwrap = false, bool? inlineParameters = null);
		/// <summary>
		/// Gets GetExpression.
		/// </summary>
		/// <summary>
		/// Gets GetExpression.
		/// </summary>
		IExpWord? GetExpression(string argName, bool unwrap = false, bool? inlineParameters = null);
		/// <summary>
		/// ConvertToSqlExpression
		/// </summary>
		/// <summary>
		/// ConvertToSqlExpression
		/// </summary>
		IExpWord? ConvertToSqlExpression();
		/// <summary>
		/// ConvertToSqlExpression
		/// </summary>
		/// <summary>
		/// ConvertToSqlExpression
		/// </summary>
		IExpWord? ConvertToSqlExpression(int        precedence);
		/// <summary>
		/// ConvertExpressionToSql
		/// </summary>
		/// <summary>
		/// ConvertExpressionToSql
		/// </summary>
		IExpWord? ConvertExpressionToSql(Expression expression, bool unwrap = false, bool? inlineParameters = null);
		/// <summary>
		/// EvaluateExpression
		/// </summary>

		object? EvaluateExpression(Expression expression);
		/// <summary>
		/// AddParameter
		/// </summary>

		SqlExtensionParam AddParameter(string name, IExpWord expr);
	}
	/// <summary>
	/// SqlExtension
	/// </summary>

	public class SqlExtension
	{
		/// <summary>
		/// NamedParameters
		/// </summary>
		/// <summary>
		/// Gets or sets NamedParameters.
		/// </summary>
		public Dictionary<string, List<SqlExtensionParam>> NamedParameters { get; }
		/// <summary>
		/// Gets or sets ChainPrecedence.
		/// </summary>

		public int ChainPrecedence { get; set; }
		/// <summary>
		/// 初始化 <see cref="SqlExtension"/> 实例。
		/// </summary>
		public SqlExtension(Type? systemType, string expr, int precedence, int chainPrecedence,
			bool isAggregate,
			bool isWindowFunction,
			bool isPure,
			bool isPredicate,
			IsNullableType isNullable,
			bool? canBeNull,
			params SqlExtensionParam[] parameters)
		{
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));

			SystemType       = systemType;
			Expr             = expr;
			Precedence       = precedence;
			ChainPrecedence  = chainPrecedence;
			IsPredicate      = isPredicate;
			IsNullable       = isNullable;
			CanBeNull        = canBeNull;
			NamedParameters  = parameters.ToLookup(static p => p.Name ?? string.Empty).ToDictionary(static p => p.Key, static p => p.ToList());

			if (isAggregate)      Flags |= SqlFlags.IsAggregate;
			if (isWindowFunction) Flags |= SqlFlags.IsWindowFunction;
			if (isPure)           Flags |= SqlFlags.IsPure;
		}
		/// <summary>
		/// Gets or sets SystemType.
		/// </summary>

		public Type?          SystemType       { get; set; }
		/// <summary>
		/// Expr
		/// </summary>
		/// <summary>
		/// Gets or sets Expr.
		/// </summary>
		public string         Expr             { get; set; }
		/// <summary>
		/// Precedence
		/// </summary>
		/// <summary>
		/// Gets or sets Precedence.
		/// </summary>
		public int            Precedence       { get; set; }
		/// <summary>
		/// IsPredicate
		/// </summary>
		/// <summary>
		/// Gets or sets IsPredicate.
		/// </summary>
		public bool           IsPredicate      { get; set; }
		/// <summary>
		/// IsNullable
		/// </summary>
		/// <summary>
		/// Gets or sets IsNullable.
		/// </summary>
		public IsNullableType IsNullable       { get; set; }
		/// <summary>
		/// CanBeNull
		/// </summary>
		/// <summary>
		/// Gets or sets CanBeNull.
		/// </summary>
		public bool?          CanBeNull        { get; set; }
		/// <summary>
		/// Gets or sets Flags.
		/// </summary>

		public SqlFlags Flags            { get; set; }
		/// <summary>
		/// IsAggregate
		/// </summary>

		public bool IsAggregate      => (Flags & SqlFlags.IsAggregate)      != 0;
		/// <summary>
		/// IsWindowFunction
		/// </summary>
		/// <summary>
		/// IsWindowFunction
		/// </summary>
		public bool IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;
		/// <summary>
		/// IsPure
		/// </summary>
		/// <summary>
		/// IsPure
		/// </summary>
		public bool IsPure           => (Flags & SqlFlags.IsPure)           != 0;
		/// <summary>
		/// AddParameter
		/// </summary>

		public SqlExtensionParam AddParameter(string name, IExpWord sqlExpression)
		{
			return AddParameter(new SqlExtensionParam(name ?? string.Empty, sqlExpression));
		}
		/// <summary>
		/// AddParameter
		/// </summary>

		public SqlExtensionParam AddParameter(SqlExtensionParam param)
		{
			var key = param.Name ?? string.Empty;

			if (!NamedParameters.TryGetValue(key, out var list))
			{
				list = new List<SqlExtensionParam>();
				NamedParameters.Add(key, list);
			}

			list.Add(param);
			return param;
		}
		/// <summary>
		/// Gets GetParametersByName.
		/// </summary>

		public IEnumerable<SqlExtensionParam> GetParametersByName(string name)
		{
			if (NamedParameters.TryGetValue(name, out var list))
				return list;
			return Enumerable.Empty<SqlExtensionParam>();
		}
		/// <summary>
		/// Gets GetParameters.
		/// </summary>

		public SqlExtensionParam[] GetParameters()
		{
			return NamedParameters.Values.SelectMany(static _ => _).ToArray();
		}
	}
	/// <summary>
	/// SqlExtensionParam
	/// </summary>

	[DebuggerDisplay("{ToDebugString()}")]
	public class SqlExtensionParam
	{
#if DEBUG
		private static int _paramCounter;
		private readonly int _paramNumber;
		/// <summary>
		/// ParamNumber
		/// </summary>
		/// <summary>
		/// ParamNumber
		/// </summary>
		public int ParamNumber => _paramNumber;
#endif
		/// <summary>
		/// 初始化 <see cref="SqlExtensionParam"/> 实例。
		/// </summary>
		public SqlExtensionParam(string? name, IExpWord expression)
		{
			Name       = name;
			Expression = expression;
#if DEBUG
			_paramNumber = Interlocked.Add(ref _paramCounter, 1);
#endif
		}
		/// <summary>
		/// 初始化 <see cref="SqlExtensionParam"/> 实例。
		/// </summary>
		public SqlExtensionParam(string? name, SqlExtension extension)
		{
			Name      = name;
			Extension = extension;
#if DEBUG
			_paramNumber = Interlocked.Add(ref _paramCounter, 1);
#endif
		}
		/// <summary>
		/// ToDebugString
		/// </summary>

		public string ToDebugString()
		{
#if DEBUG
			var paramPrefix = $"Param[{ParamNumber.ToString(CultureInfo.InvariantCulture)}]";
#else
			var paramPrefix = "Param";
#endif

			if (Extension != null)
				return $"{paramPrefix}('{Name ?? ""}', {Extension.ChainPrecedence}): {Extension.Expr}";

			if (Expression != null)
				return $"{paramPrefix}('{Name ?? ""}'): {Expression.ToDebugString()}";

			return $"{paramPrefix}('{Name ?? ""}')";
		}
		/// <summary>
		/// Gets or sets Name.
		/// </summary>

		public string?         Name       { get; set; }
		/// <summary>
		/// Extension
		/// </summary>
		/// <summary>
		/// Gets or sets Extension.
		/// </summary>
		public SqlExtension?   Extension  { get; set; }
		/// <summary>
		/// Expression
		/// </summary>
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>
		public IExpWord? Expression { get; set; }
	}
}
