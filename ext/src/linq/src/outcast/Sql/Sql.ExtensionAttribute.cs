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

	public enum ExprParameterKind
	{
		Default,
		Sequence,
		Values
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	
	public class ExprParameterAttribute : Attribute
	{
		public string?           Name              { get; set; }
		public ExprParameterKind ParameterKind     { get; set; }
		public bool              DoNotParameterize { get; set; }

		public ExprParameterAttribute(string name)
		{
			Name = name;
		}

		public ExprParameterAttribute()
		{
		}
	}

	public static class ExtensionBuilderExtensions
	{
		public static Sql.SqlExtensionParam AddParameter(this Sql.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new ValueWord(value));
		}

		public static Sql.SqlExtensionParam AddExpression(this Sql.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new ExpressionWord(expr, PrecedenceLv.Primary));
		}

		public static IExpWord Add(this Sql.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "+", right, PrecedenceLv.Additive);
		}
		public static IExpWord Add<T>(this Sql.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Add(left, right, typeof(T));
		}

		public static IExpWord Add(this Sql.ISqExtensionBuilder builder, IExpWord left, int value)
		{
			return builder.Add<int>(left, new ValueWord(value));
		}

		public static IExpWord Inc(this Sql.ISqExtensionBuilder builder, IExpWord expr)
		{
			return builder.Add(expr, 1);
		}

		public static IExpWord Sub(this Sql.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "-", right, PrecedenceLv.Subtraction);
		}

		public static IExpWord Sub<T>(this Sql.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Sub(left, right, typeof(T));
		}

		public static IExpWord Sub(this Sql.ISqExtensionBuilder builder, IExpWord left, int value)
		{
			return builder.Sub<int>(left, new ValueWord(value));
		}

		public static IExpWord Dec(this Sql.ISqExtensionBuilder builder, IExpWord expr)
		{
			return builder.Sub(expr, 1);
		}

		public static IExpWord Mul(this Sql.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "*", right, PrecedenceLv.Multiplicative);
		}

		public static IExpWord Mul<T>(this Sql.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Mul(left, right, typeof(T));
		}

		public static IExpWord Mul(this Sql.ISqExtensionBuilder builder, IExpWord expr1, int value)
		{
			return builder.Mul<int>(expr1, new ValueWord(value));
		}

		public static IExpWord Div(this Sql.ISqExtensionBuilder builder, IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "/", expr2, PrecedenceLv.Multiplicative);
		}

		public static IExpWord Div<T>(this Sql.ISqExtensionBuilder builder, IExpWord expr1, IExpWord expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}

		public static IExpWord Div(this Sql.ISqExtensionBuilder builder, IExpWord expr1, int value)
		{
			return builder.Div<int>(expr1, new ValueWord(value));
		}
	}

	public partial class Sql
	{
		public interface ISqlExtension
		{
		}

		public static ISqlExtension? Ext => null;

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
		public class ExtensionAttribute : ExpressionAttribute
		{
			private static readonly ConcurrentDictionary<Type, IExtensionCallBuilder> _builders = new ();

			public string? TokenName { get; set; }

			protected class ExtensionBuilder<TContext>: ISqExtensionBuilder
			{
				readonly TContext              _context;
				readonly ConvertFunc<TContext> _convert;

				public ExtensionBuilder(
					TContext              context,
					IExpressionEvaluator  evaluator,
					string?               configuration,
					object?               builderValue,
					DBInstance          dataContext,
                    SelectQueryClause           query,
					SqlExtension          extension,
					ConvertFunc<TContext> converter,
					MemberInfo            member,
					Expression[]          arguments,
					IsNullableType        isNullable,
					bool?                 canBeNull)
				{
					_context      = context;
					Evaluator     = evaluator;
					Configuration = configuration;
					BuilderValue  = builderValue;
					Query         = query       ?? throw new ArgumentNullException(nameof(query));
					Extension     = extension   ?? throw new ArgumentNullException(nameof(extension));
					_convert      = converter   ?? throw new ArgumentNullException(nameof(converter));
					Member        = member;
					Method        = member as MethodInfo;
					Arguments     = arguments ?? throw new ArgumentNullException(nameof(arguments));
					IsNullable    = isNullable;
					CanBeNull     = canBeNull;
				}

				public MethodInfo?  Method { get; }

				public IExpWord? ConvertExpression(Expression expr, bool unwrap, EntityColumn? columnDescriptor, bool? inlineParameters)
				{
					if (unwrap)
						expr = expr.UnwrapConvert();

					var converted = _convert(_context, expr, columnDescriptor, inlineParameters);
					if (converted is SqlPlaceholderExpression placeholder)
						return placeholder.Sql;

					return null;
				}

				#region ISqExtensionBuilder Members

				public IExpressionEvaluator Evaluator        { get; }
				public string?              Configuration    { get; }
				public object?              BuilderValue     { get; }

                public DBInstance DBLive { get; }
				public SelectQueryClause          Query            { get; }
				public MemberInfo           Member           { get; }
				public SqlExtension         Extension        { get; }
				public IExpWord?      ResultExpression { get; set; }
				public bool                 IsConvertible    { get; set; } = true;
				public Expression[]         Arguments        { get; }
				public IsNullableType       IsNullable       { get; }
				public bool?                CanBeNull        { get; }

				public string Expression
				{
					get => Extension.Expr;
					set => Extension.Expr = value;
				}

				public T GetValue<T>(int index)
				{
					var value = (T)Evaluator.Evaluate(Arguments[index])!;
					return value;
				}

				public T GetValue<T>(string argName)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
							if (parameters[i].Name == argName)
								return GetValue<T>(i);
					}

					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Argument '{0}' not found", argName));
				}

				public object GetObjectValue(int index)
				{
					var value = Evaluator.Evaluate(Arguments[index])!;
					return value;
				}

				public object GetObjectValue(string argName)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
							if (parameters[i].Name == argName)
								return GetObjectValue(i);
					}

					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Argument '{0}' not found", argName));
				}

				public IExpWord? GetExpression(int index, bool unwrap, bool? inlineParameters = null)
				{
					return ConvertExpression(Arguments[index], unwrap, null, inlineParameters);
				}

				public IExpWord? GetExpression(string argName, bool unwrap, bool? inlineParameters = null)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
						{
							if (parameters[i].Name == argName)
							{
								return GetExpression(i, unwrap);
							}
						}
					}

					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Argument '{0}' not found", argName));
				}

				public IExpWord? ConvertToSqlExpression()
				{
					return ConvertToSqlExpression(Extension.Precedence);
				}

				public IExpWord? ConvertToSqlExpression(int precedence)
				{
					var converted = BuildSqlExpression(Query, Extension, Extension.SystemType!, precedence,
						(Extension.IsAggregate      ? SqlFlags.IsAggregate      : SqlFlags.None) |
						(Extension.IsPure           ? SqlFlags.IsPure           : SqlFlags.None) |
						(Extension.IsPredicate      ? SqlFlags.IsPredicate      : SqlFlags.None) |
						(Extension.IsWindowFunction ? SqlFlags.IsWindowFunction : SqlFlags.None),
						Extension.CanBeNull, IsNullableType.Undefined);

					if (converted is SqlPlaceholderExpression placeholder)
						return placeholder.Sql;

					return null;
				}

				public IExpWord? ConvertExpressionToSql(Expression expression, bool unwrap, bool? inlineParameters = null)
				{
					return ConvertExpression(expression, unwrap, null, inlineParameters);
				}

				public object? EvaluateExpression(Expression expression)
				{
					if (expression == null)
						return null;

					return Evaluator.Evaluate(expression);
				}

				public SqlExtensionParam AddParameter(string name, IExpWord expr)
				{
					return Extension.AddParameter(name, expr);
				}

				#endregion

			}

			public Type?     BuilderType     { get; set; }
			public object?   BuilderValue    { get; set; }

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
				MemberInfo memberInfo;

				switch (expression.NodeType)
				{
					case ExpressionType.MemberAccess:
						memberInfo = ((MemberExpression) expression).Member;
						break;
					case ExpressionType.Call:
						memberInfo = ((MethodCallExpression) expression).Method;
						break;
					default:
						return new ExtensionAttribute[] { };
				}

				//var attributes = mapping.GetAttributes<ExtensionAttribute>(memberInfo.ReflectedType!, memberInfo, forFirstConfiguration: forFirstConfiguration);

				//return attributes;
				return new ExtensionAttribute[] { };
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

			protected List<SqlExtensionParam>? BuildFunctionsChain<TContext>(TContext context, DBInstance DB, IExpressionEvaluator evaluator, SelectQueryClause query, Expression expr, ConvertFunc<TContext> converter, out Expression? error)
			{
				error = null;
				var chains           = new List<SqlExtensionParam>();
				Expression? current  = expr;

				while (current != null)
				{
					MemberInfo?   memberInfo = null;
					Expression[]? arguments  = null;
					Expression?   next       = null;

					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var memberExpr = (MemberExpression)current;

								memberInfo = memberExpr.Member;
								arguments  = new Expression [] { };
								next       = memberExpr.Expression;

								break;
							}

						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;

								memberInfo = call.Method;
								arguments  = call.Arguments.ToArray();

								if (call.Method.IsStatic)
									next = call.Arguments.FirstOrDefault();
								else
									next = call.Object;

								break;
							}

						case ExpressionType.Constant:
							{
								if (typeof(IQueryableContainer).IsSameOrParentOf(current.Type))
								{
									next = current.EvaluateExpression<IQueryableContainer>()!.Query.Expression;
								}
								break;
							}
					}

					if (memberInfo != null)
					{
						var attributes = GetExtensionAttributes(current, DB);
						var tokenNames = attributes.Where(a => !string.IsNullOrEmpty(a.TokenName))
							.Select(a => a.TokenName!).ToList();
						var namedAttributes = GetExtensionAttributes(current, DB, false)
							.Where(e => !string.IsNullOrEmpty(e.TokenName) && !tokenNames.Contains(e.TokenName!));

						var continueChain   = false;

						foreach (var attr in attributes.Concat(namedAttributes))
						{
							var param = attr.BuildExtensionParam(context, expr, DB, evaluator, query, memberInfo, arguments!, converter, out error);

							if (param == null)
							{
								return null;
							}

							continueChain = continueChain || !string.IsNullOrEmpty(param.Name) ||
							                param.Extension != null && param.Extension.ChainPrecedence != -1;
							chains.Add(param);
						}

						if (!continueChain)
							break;
					}

					current = next;
				}

				return chains;
			}

			SqlExtensionParam? BuildExtensionParam<TContext>(TContext context, Expression extensionExpression, DBInstance dataContext, IExpressionEvaluator evaluator, SelectQueryClause query, MemberInfo member, Expression[] arguments, ConvertFunc<TContext> converter, out Expression? error)
			{
				var method = member as MethodInfo;
				var type   = member.GetMemberType();
				if (method != null)
					type = method.ReturnType ?? type;
				else if (member is PropertyInfo)
					type = ((PropertyInfo)member).PropertyType;

				var extension = new SqlExtension(type, Expression!, Precedence, ChainPrecedence, IsAggregate, IsWindowFunction, IsPure, IsPredicate, IsNullable, _canBeNull);

				SqlExtensionParam? result = null;

				if (method != null)
				{
					var parameters = method.GetParameters();

					var genericDefinition        = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
					var templateParameters       = genericDefinition.GetParameters();
					var templateGenericArguments = genericDefinition.GetGenericArguments();
					var descriptorMapping        = new Dictionary<Type, EntityColumn?>();

					for (var i = 0; i < parameters.Length; i++)
					{
						var arg   = arguments[i];
						var param = parameters[i];

						bool? inlineParameters = null;

						var names = new HashSet<string>();
						foreach (var a in param.GetAttributes<ExprParameterAttribute>())
						{
							if (a.DoNotParameterize)
								inlineParameters = true;

							names.Add(a.Name ?? param.Name!);
						}

						if (names.Count > 0)
						{
							if (method.IsGenericMethod)
							{
								var templateParam  = templateParameters[i];
								var elementType    = templateParam.ParameterType!;
								var argElementType = param.ParameterType;
								descriptorMapping.TryGetValue(elementType, out var descriptor);

								Expression[] sqlExpressions;
								if (arg is NewArrayExpression arrayInit)
								{
									sqlExpressions = new Expression[arrayInit.Expressions.Count];
									for (var j = 0; j < sqlExpressions.Length; j++)
										sqlExpressions[j] = converter(context, arrayInit.Expressions[j], descriptor, inlineParameters);
								}
								else
								{
									var callDescriptor = descriptor;

									if (callDescriptor != null && callDescriptor.UnderType != arg.Type && !(callDescriptor.UnderType.IsAssignableFrom(arg.Type) || arg.Type.IsAssignableFrom(callDescriptor.UnderType)))
									{
										callDescriptor = null;
									}

									var sqlExpression = converter(context, arg, callDescriptor, inlineParameters);
									sqlExpressions = new[] { sqlExpression };
								}

								if (descriptor == null)
								{
									descriptor = sqlExpressions.OfType<SqlPlaceholderExpression>().Select(p => QueryHelper.GetColumnDescriptor(p.Sql)).FirstOrDefault(static d => d != null);
									if (descriptor != null)
									{
										foreach (var pair
										         in TypeHelper.EnumTypeRemapping(elementType, argElementType, templateGenericArguments))
										{
#if NET6_0_OR_GREATER
											descriptorMapping.TryAdd(pair.Item1, descriptor);
#else
											if (!descriptorMapping.ContainsKey(pair.Item1))
												descriptorMapping.Add(pair.Item1, descriptor);
#endif
										}
									}
								}

								foreach (var name in names)
								foreach (var expr in sqlExpressions)
								{
									if (expr is not SqlPlaceholderExpression placeholder)
									{
										error = expr;
										return null;
									}

									extension.AddParameter(name!, placeholder.Sql);
								}
							}
							else
							{
								Expression?   sqlExpression  = null;
								Expression[]? sqlExpressions = null;
								if (arg is NewArrayExpression arrayInit)
								{
									sqlExpressions = new Expression[arrayInit.Expressions.Count];
									for (var j = 0; j < sqlExpressions.Length; j++)
										sqlExpressions[j] = converter(context, arrayInit.Expressions[j], null, inlineParameters);
								}
								else
									sqlExpression = converter(context, arg, null, inlineParameters);

								foreach (var name in names)
									if (sqlExpressions != null)
									{
										foreach (var sqlExpr in sqlExpressions)
										{
											if (sqlExpr is not SqlPlaceholderExpression placeholder)
											{
												error = sqlExpr;
												return null;
											}
											extension.AddParameter(name!, placeholder.Sql);
										}
									}
									else
									{
										if (sqlExpression is not SqlPlaceholderExpression placeholder)
										{
											error = sqlExpression;
											return null;
										}
										extension.AddParameter(name!, placeholder.Sql);
									}
							}
						}
					}
				}

				if (BuilderType != null)
				{
					var callBuilder = _builders.GetOrAdd(BuilderType, static t =>
						{
							if (Activator.CreateInstance(t)! is IExtensionCallBuilder res)
								return res;

							throw new ArgumentException(
								$"Type '{t}' does not implement {nameof(IExtensionCallBuilder)} interface.");
						}
					);

					var builder = new ExtensionBuilder<TContext>(context, evaluator, Configuration, BuilderValue, dataContext,
						query, extension, converter, member, arguments, IsNullable, _canBeNull);

					callBuilder.Build(builder);

					if (!builder.IsConvertible)
					{
						error = extensionExpression;
						return null;
					}

					result = builder.ResultExpression != null ?
						new SqlExtensionParam(TokenName, builder.ResultExpression) :
						new SqlExtensionParam(TokenName, builder.Extension);
				}

				error  =   null;
				result ??= new SqlExtensionParam(TokenName, extension);

				return result;
			}

			static IEnumerable<Expression> ExtractArray(Expression expression)
			{
				var array = (NewArrayExpression) expression;
				return array.Expressions;
			}

			public static Expression BuildSqlExpression(SelectQueryClause query, SqlExtension root, Type systemType, int precedence,
				SqlFlags flags, bool? canBeNull, IsNullableType isNullable)
			{
				var resolvedParams = new Dictionary<SqlExtensionParam, string?>();
				var resolving      = new HashSet<SqlExtensionParam>();
				var newParams      = new List<IExpWord>();

				Expression? valueProviderError = null;

				Func<object?, string, string?, string?>? valueProvider = null;
				Stack<SqlExtension>                      current       = new Stack<SqlExtension>();

				// TODO: implement context
				valueProvider = (_, name, delimiter) =>
				{
					var found = root.GetParametersByName(name);
					if (current.Count != 0)
						found = current.Peek().GetParametersByName(name).Concat(found);
					string? result = null;
					foreach (var p in found)
					{
						if (resolvedParams.TryGetValue(p, out var paramValue))
						{
							result = paramValue;
						}
						else
						{
							if (resolving.Contains(p))
								throw new InvalidOperationException("Circular reference");

							resolving.Add(p);
							var ext = p.Extension;
							if (ext != null)
							{
								current.Push(ext);
								paramValue = ResolveExpressionValues(_, ext.Expr, valueProvider!, out var error);
								valueProviderError ??= error;
								current.Pop();
							}
							else
							{
								if (p.Expression != null)
								{
									paramValue = string.Format(CultureInfo.InvariantCulture, "{{{0}}}", newParams.Count);
									newParams.Add(p.Expression);
								}
							}

							resolvedParams.Add(p, paramValue);

							if (string.IsNullOrEmpty(paramValue))
								continue;

							if (!string.IsNullOrEmpty(result))
								result += delimiter;
							result += paramValue;
						}

						if (delimiter == null && !string.IsNullOrEmpty(result))
							break;
					}

					return result;
				};

				var expr = ResolveExpressionValues(null, root.Expr, valueProvider, out var error);

				if (valueProviderError != null)
					return valueProviderError;

				if (error != null)
					return error;

				var sqlExpression = new ExpressionWord(systemType, expr, precedence, flags,
                    ToParametersNullabilityType(isNullable), canBeNull, newParams.ToArray());

				// Placeholder path will be set later
				return ExpressionBuilder.CreatePlaceholder(query, sqlExpression, System.Linq.Expressions.Expression.Default(systemType));
			}

			public override Expression GetExpression<TContext>(TContext context, DBInstance DB, IExpressionEvaluator evaluator, SelectQueryClause query, Expression expression, ConvertFunc<TContext> converter)
			{
				// chain starts from the tail
				var chain  = BuildFunctionsChain(context, DB, evaluator, query, expression, converter, out var error);

				if (chain == null)
					return expression;

				if (chain.Count == 0)
					throw new InvalidOperationException($"No sequence found for expression '{expression}'");

				var ordered = chain
					.Select(static (c, i) => Tuple.Create(c, i))
					.OrderByDescending(static t => t.Item1.Extension?.ChainPrecedence ?? int.MinValue)
					.ThenByDescending(static t => t.Item2)
					.Select(static t => t.Item1)
					.ToArray();

				var main    = ordered.FirstOrDefault(static c => c.Extension != null);

				if (main == null)
				{
					var replaced = chain.Where(static c => c.Expression != null).ToArray();
					if (replaced.Length == 0)
						throw new InvalidOperationException($"Cannot find root sequence for expression '{expression}'");
					else if (replaced.Length > 1)
						throw new InvalidOperationException($"Multiple root sequences found for expression '{expression}'");

					return ExpressionBuilder.CreatePlaceholder(query, replaced[0].Expression!, expression);
				}

				var mainExtension = main.Extension!;

				// suggesting type
				mainExtension.SystemType = expression.Type;

				// calculating that extension is aggregate
				var isAggregate  = ordered.Any(static c => c.Extension?.IsAggregate == true);
				var isPure       = ordered.All(static c => c.Extension?.IsPure != false);
				var isWindowFunc = ordered.Any(static c => c.Extension?.IsWindowFunction == true);
				var isPredicate  = mainExtension.IsPredicate;

				// calculating replacements
				var replacementMap = ordered
					.Where(c => c.Extension != mainExtension)
					.Select(static (c, i) => Tuple.Create(c, i))
					.GroupBy(static e => e.Item1.Name ?? "")
					.Select(static g => new
					{
						Name = g.Key,
						UnderName = g
							.OrderByDescending(static e => e.Item1.Extension?.ChainPrecedence ?? int.MinValue)
							.ThenBy(static e => e.Item2)
							.Select(static e => e.Item1)
							.ToArray()
					})
					.ToArray();

				foreach (var c in replacementMap)
				{
					var first = c.UnderName[0];
					if (c.Name.Length == 0 || first.Extension == null)
					{
						for (var i = 0; i < c.UnderName.Length; i++)
						{
							var e = c.UnderName[i];
							mainExtension.AddParameter(e);
						}
					}
					else
					{
						var firstPrecedence = first.Extension.ChainPrecedence;
						mainExtension.AddParameter(first);
						// append all replaced under superior
						for (int i = 1; i < c.UnderName.Length; i++)
						{
							var item = c.UnderName[i];
							if (firstPrecedence > (item.Extension?.ChainPrecedence ?? int.MinValue))
								first.Extension.AddParameter(item);
							else
								mainExtension.AddParameter(item);
						}
					}
				}

				// TODO: Really not precise nullability calculation. In the future move to window functions to MemberTranslator
				var canBeNull = mainExtension.CanBeNull;
				if (canBeNull == null)
				{
					foreach (var c in ordered)
					{
						if (c.Extension != null && c.Extension.CanBeNull != null)
						{
							canBeNull = c.Extension.CanBeNull;
							break;
						}
					}
				}

				//TODO: Precedence calculation
				var res = BuildSqlExpression(query, mainExtension, mainExtension.SystemType,
					mainExtension.Precedence,
					(isAggregate  ? SqlFlags.IsAggregate      : SqlFlags.None) |
					(isPure       ? SqlFlags.IsPure           : SqlFlags.None) |
					(isPredicate  ? SqlFlags.IsPredicate      : SqlFlags.None) |
					(isWindowFunc ? SqlFlags.IsWindowFunction : SqlFlags.None),
					canBeNull, IsNullable);

				return res;
			}

			public override string GetObjectID()
			{
				return $"{base.GetObjectID()}.{TokenName}.{IdentifierBuilder.GetObjectID(BuilderType)}.{BuilderValue}.{ChainPrecedence}.";
			}
		}
	}
}
