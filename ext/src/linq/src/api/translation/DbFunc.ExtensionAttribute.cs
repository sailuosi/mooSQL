using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;


namespace mooSQL.linq
{
	using Common;
	using Common.Internal;
	using Expressions;
	using Extensions;
	using Linq.Builder;
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.utils;
    using SqlQuery;

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
				string str;

#if DEBUG
				var paramPrefix = $"Param[{ParamNumber.ToString(CultureInfo.InvariantCulture)}]";
#else
				var paramPrefix = $"Param";
#endif

				if (Extension != null)
				{
					str =$"{paramPrefix}('{Name ?? ""}', {Extension.ChainPrecedence}): {Extension.Expr}";
				}
				else if (Expression != null)
				{
					var sb = new QueryElementTextWriter();
					Expression.ToString();
					str = $"{paramPrefix}('{Name ?? ""}'): {sb}";
				}
				else
					str = $"{paramPrefix}('{Name ?? ""}')";

				return str;
			}

			public string?         Name       { get; set; }
			public SqlExtension?   Extension  { get; set; }
			public IExpWord? Expression { get; set; }
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public partial class ExtensionAttribute : ExpressionAttribute
		{
			public Type?     BuilderType     { get; set; }
			public object?   BuilderValue    { get; set; }

			/// <summary>编译时按 <see cref="NullsPosition"/> 参数追加 NULLS FIRST/LAST（替代 OrderItemBuilder）。</summary>
			public bool      AppendNullsPositionSuffix { get; set; }

			/// <summary>
			/// Defines in which order process extensions. Items will be ordered Descending.
			/// </summary>
			public int       ChainPrecedence { get; set; }

			public ExtensionAttribute(string expression): this(string.Empty, expression)
			{
			}

			public ExtensionAttribute(string configuration, string expression) : base(configuration, expression)
			{
				ExpectExpression = true;
				ServerSideOnly   = true;
				PreferServerSide = true;
				ChainPrecedence  = -1;
			}

			public ExtensionAttribute(Type builderType): this(string.Empty, builderType)
			{
			}

			public ExtensionAttribute(string configuration, Type builderType) : this(configuration, string.Empty)
			{
				BuilderType = builderType;
			}

			public static ExtensionAttribute[] GetExtensionAttributes(Expression expression, DBInstance mapping, bool forFirstConfiguration = true)
			{
				MemberInfo? memberInfo = expression.NodeType switch
				{
					ExpressionType.MemberAccess => ((MemberExpression)expression).Member,
					ExpressionType.Call         => ((MethodCallExpression)expression).Method,
					_                           => null
				};

				if (memberInfo == null)
					return Array.Empty<ExtensionAttribute>();

				var all = memberInfo.GetAttributes<ExtensionAttribute>(inherit: true);
				if (all.Length == 0)
					return all;

				var configuration = MappingExtensions.GetDialectConfiguration(mapping);
				var primary       = PickExtensionAttributes(all, configuration);

				if (forFirstConfiguration)
					return primary;

				var primaryTokens = new HashSet<string>(
					primary.Where(a => !string.IsNullOrEmpty(a.TokenName)).Select(a => a.TokenName!));

				return all
					.Where(a => !string.IsNullOrEmpty(a.TokenName) && !primaryTokens.Contains(a.TokenName!))
					.Where(a => !string.IsNullOrEmpty(a.Configuration)
					            && (configuration == null || !string.Equals(a.Configuration, configuration, StringComparison.OrdinalIgnoreCase)))
					.ToArray();
			}

			static ExtensionAttribute[] PickExtensionAttributes(ExtensionAttribute[] attrs, string? configuration)
			{
				if (attrs.Length <= 1)
					return attrs;

				if (configuration != null)
				{
					var dialectSpecific = attrs
						.Where(a => string.Equals(a.Configuration, configuration, StringComparison.OrdinalIgnoreCase))
						.ToArray();
					if (dialectSpecific.Length > 0)
						return dialectSpecific;
				}

				var defaults = attrs.Where(a => string.IsNullOrEmpty(a.Configuration)).ToArray();
				return defaults.Length > 0 ? defaults : attrs;
			}

			public static Expression ExcludeExtensionChain(DBInstance mapping, Expression expr, out bool isQueryable)
			{
				var current = expr;
				isQueryable = false;

				while (true)
				{
					var attributes = GetExtensionAttributes(current, mapping);

					if (attributes.Length == 0)
						break;

					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var memberExpr = (MemberExpression)current;
								current        = memberExpr.Expression!;

								break;
							}

						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;

								if (call.Method.IsStatic && call.Method.DeclaringType != null)
								{
									isQueryable = false;
									var firstArgType = call.Arguments[0].Type;
									if (call.Arguments.Count > 0 && typeof(IQueryableContainer).IsSameOrParentOf(firstArgType) || typeof(IEnumerable<>).IsSameOrParentOf(firstArgType))
									{
										var paramAttribute = call.Method.GetParameters()[0].GetAttribute<ExprParameterAttribute>();
										if (paramAttribute == null ||
										    paramAttribute.ParameterKind == ExprParameterKind.Default ||
										    paramAttribute.ParameterKind == ExprParameterKind.Sequence)
										{
											current     = call.Arguments[0];
											isQueryable = typeof(IQueryableContainer).IsSameOrParentOf(current.Type);
										}
										else
											return current;
									}
									else
										return current;
								}
								else
									current = call.Object!;

								break;
							}
						default:
							{
								return current;
							}
					}
				}

				return current;
			}

		}
	}
}
