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
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq;

public partial class DbFunc
{
	[Obsolete("Use SooFunctionExtension.ISqlExtension.")]
	public interface ISqlExtension : SooFunctionExtension.ISqlExtension
	{
	}

	[Obsolete("Use SooFunc.Ext.")]
	public static SooFunctionExtension.ISqlExtension? Ext => SooFunc.Ext;

	public interface IExtensionCallBuilder
	{
		void Build(ISqExtensionBuilder builder);
	}

	public interface IQueryableContainer
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		IQueryable Query { get; }
	}

	public interface ISqExtensionBuilder
	{
		string?         Configuration    { get; }
		object?         BuilderValue     { get; }

		DBInstance DBLive { get; }
		SelectQueryClause     Query            { get; }
		MemberInfo      Member           { get; }
		SqlExtension    Extension        { get; }
		IExpWord? ResultExpression { get; set; }
		bool            IsConvertible    { get; set; }
		string          Expression       { get; set; }
		Expression[]    Arguments        { get; }

		IsNullableType  IsNullable       { get; }
		bool?           CanBeNull        { get; }

		T      GetValue<T>   (int    index);
		T      GetValue<T>   (string argName);
		object GetObjectValue(int    index);
		object GetObjectValue(string argName);

		IExpWord? GetExpression(int    index,   bool unwrap = false, bool? inlineParameters = null);
		IExpWord? GetExpression(string argName, bool unwrap = false, bool? inlineParameters = null);
		IExpWord? ConvertToSqlExpression();
		IExpWord? ConvertToSqlExpression(int        precedence);
		IExpWord? ConvertExpressionToSql(Expression expression, bool unwrap = false, bool? inlineParameters = null);

		object? EvaluateExpression(Expression expression);

		SqlExtensionParam AddParameter(string name, IExpWord expr);
	}

	public class SqlExtension
	{
		public Dictionary<string, List<SqlExtensionParam>> NamedParameters { get; }

		public int ChainPrecedence { get; set; }

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

		public Type?          SystemType       { get; set; }
		public string         Expr             { get; set; }
		public int            Precedence       { get; set; }
		public bool           IsPredicate      { get; set; }
		public IsNullableType IsNullable       { get; set; }
		public bool?          CanBeNull        { get; set; }

		public SqlFlags Flags            { get; set; }

		public bool IsAggregate      => (Flags & SqlFlags.IsAggregate)      != 0;
		public bool IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;
		public bool IsPure           => (Flags & SqlFlags.IsPure)           != 0;

		public SqlExtensionParam AddParameter(string name, IExpWord sqlExpression)
		{
			return AddParameter(new SqlExtensionParam(name ?? string.Empty, sqlExpression));
		}

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

		public IEnumerable<SqlExtensionParam> GetParametersByName(string name)
		{
			if (NamedParameters.TryGetValue(name, out var list))
				return list;
			return Enumerable.Empty<SqlExtensionParam>();
		}

		public SqlExtensionParam[] GetParameters()
		{
			return NamedParameters.Values.SelectMany(static _ => _).ToArray();
		}
	}

	[DebuggerDisplay("{ToDebugString()}")]
	public class SqlExtensionParam
	{
#if DEBUG
		private static int _paramCounter;
		private readonly int _paramNumber;
		public int ParamNumber => _paramNumber;
#endif

		public SqlExtensionParam(string? name, IExpWord expression)
		{
			Name       = name;
			Expression = expression;
#if DEBUG
			_paramNumber = Interlocked.Add(ref _paramCounter, 1);
#endif
		}

		public SqlExtensionParam(string? name, SqlExtension extension)
		{
			Name      = name;
			Extension = extension;
#if DEBUG
			_paramNumber = Interlocked.Add(ref _paramCounter, 1);
#endif
		}

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

		public string?         Name       { get; set; }
		public SqlExtension?   Extension  { get; set; }
		public IExpWord? Expression { get; set; }
	}
}
