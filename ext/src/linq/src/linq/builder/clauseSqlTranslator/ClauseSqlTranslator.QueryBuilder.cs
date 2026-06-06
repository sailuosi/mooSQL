using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using Common;
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
	using Reflection;
    using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.data;

    partial class ClauseSqlTranslator
	{
		#region BuildExpression

        static bool GetParentQuery(Dictionary<SelectQueryClause, SelectQueryClause> parentInfo, SelectQueryClause currentQuery, [MaybeNullWhen(false)] out SelectQueryClause? parentQuery)
		{
			return parentInfo.TryGetValue(currentQuery, out parentQuery);
		}

		public class ParentInfo
		{
			Dictionary<SelectQueryClause, SelectQueryClause>? _info;

			public bool GetParentQuery(SelectQueryClause rootQuery, SelectQueryClause currentQuery, [MaybeNullWhen(false)] out SelectQueryClause? parentQuery)
			{
				if (_info == null)
				{
					_info = new(Utils.ObjectReferenceEqualityComparer<SelectQueryClause>.Default);
					BuildParentsInfo(rootQuery, _info);
				}
				return _info.TryGetValue(currentQuery, out parentQuery);
			}

			static void BuildParentsInfo(SelectQueryClause selectQuery, Dictionary<SelectQueryClause, SelectQueryClause> parentInfo)
			{
				foreach (var ts in selectQuery.From.Tables)
				{
					if (ts.FindISrc() is SelectQueryClause sc)
					{
						parentInfo[sc] = selectQuery;
						BuildParentsInfo(sc, parentInfo);
					}

					foreach (var join in ts.GetJoins())
					{
						if (join.Table.FindISrc() is SelectQueryClause jc)
						{
							parentInfo[jc] = selectQuery;
							BuildParentsInfo(jc, parentInfo);
						}
					}
				}
			}

			public void Cleanup()
			{
				_info = null;
			}
		}

		public TExpression UpdateNesting<TExpression>(IClauseContext upToContext, TExpression expression)
			where TExpression : Expression
		{
			var corrected = UpdateNesting(upToContext.SelectQuery, expression);
			
			return corrected;
		}

		public TExpression UpdateNesting<TExpression>(SelectQueryClause upToQuery, TExpression expression)
			where TExpression : Expression
		{
			using var parentInfo = ParentInfoPool.Allocate();

			var corrected = UpdateNestingInternal(upToQuery, expression, parentInfo.Value);

			return corrected;
		}

		TExpression UpdateNestingInternal<TExpression>(SelectQueryClause upToQuery, TExpression expression, ParentInfo parentInfo)
			where TExpression : Expression
		{
			// short path
			if (expression is SqlPlaceholderExpression currentPlaceholder && currentPlaceholder.SelectQuery == upToQuery)
				return expression;

			var withColumns =
				expression.Transform(
					(builder: this, upToQuery, parentInfo),
					static (context, expr) =>
					{
						if (expr is SqlPlaceholderExpression placeholder && !ReferenceEquals(context.upToQuery, placeholder.SelectQuery))
						{
							do
							{
								if (placeholder.SelectQuery == null)
									break;

								if (ReferenceEquals(context.upToQuery, placeholder.SelectQuery))
									break;

								if (!context.parentInfo.GetParentQuery(context.upToQuery, placeholder.SelectQuery, out var parentQuery))
									break;

								placeholder = context.builder.MakeColumn(parentQuery, placeholder);
							} while (true);

							return placeholder;
						}

						return expr;
					});

			return (TExpression)withColumns;
		}

		public Expression ToColumns(IClauseContext rootContext, Expression expression)
		{
			return ToColumns(rootContext.SelectQuery, expression);
		}

		public Expression ToColumns(SelectQueryClause rootQuery, Expression expression)
		{
			using var parentInfo = ParentInfoPool.Allocate();

			var withColumns =
				expression.Transform(
					(builder: this, parentInfo: parentInfo.Value, rootQuery),
					static (context, expr) =>
					{
						if (expr is SqlPlaceholderExpression { SelectQuery: { } } placeholder)
						{
							do
							{
								if (placeholder.SelectQuery == null)
									break;

								if (placeholder.Sql is RowWord)
								{
									throw new SooQueryException("DbFunc.Row(...) cannot be top level expression.");
								}

								if (ReferenceEquals(placeholder.SelectQuery, context.rootQuery))
								{
									placeholder = context.builder.MakeColumn(null, placeholder);
									break;
								}

								if (!context.parentInfo.GetParentQuery(context.rootQuery, placeholder.SelectQuery, out var parentQuery))
								{
									// Handling OUTPUT cases
									//
									placeholder = context.builder.MakeColumn(null, placeholder);
									break;
								}

								placeholder = context.builder.MakeColumn(parentQuery, placeholder);

							} while (true);

							return placeholder;
						}

						return expr;
					});

			return withColumns;
		}

		public bool TryConvertToSql(IClauseContext? context, Expression expression, ProjectFlags flags,
			EntityColumn? columnDescriptor, [NotNullWhen(true)] out IExpWord? sqlExpression,
			[NotNullWhen(false)] out SqlErrorExpression? error)
		{
			flags = flags & ~ProjectFlags.Expression | ProjectFlags.SQL;

			sqlExpression = null;

			//Just test that we can convert
			var actual = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, unwrap: false, columnDescriptor : columnDescriptor);
			if (actual is not SqlPlaceholderExpression placeholderTest)
			{
				error = SqlErrorExpression.EnsureError(context, expression);
				return false;
			};

			sqlExpression = placeholderTest.Sql;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				sqlExpression = null;
				//Test conversion success, do it again
				var newActual = ConvertToSqlExpr(context, expression, flags, columnDescriptor : columnDescriptor);
				if (newActual is not SqlPlaceholderExpression placeholder)
				{
					error = SqlErrorExpression.EnsureError(context, expression);
					return false;
				}

				sqlExpression = placeholder.Sql;
			}

			error = null;
			return true;
		}

		public SqlPlaceholderExpression? TryConvertToSqlPlaceholder(IClauseContext context, Expression expression, ProjectFlags flags, EntityColumn? columnDescriptor = null)
		{
			flags |= ProjectFlags.SQL;
			flags &= ~ProjectFlags.Expression;

			//Just test that we can convert
			var converted = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, columnDescriptor : columnDescriptor);
			if (converted is not SqlPlaceholderExpression)
				return null;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				//Test conversion success, do it again
				converted = ConvertToSqlExpr(context, expression, flags, columnDescriptor : columnDescriptor);
				if (converted is not SqlPlaceholderExpression)
					return null;
			}

			return (SqlPlaceholderExpression)converted;
		}

		public static SqlErrorExpression CreateSqlError(IClauseContext? context, Expression expression)
		{
			return new SqlErrorExpression(context, expression);
		}

		public static bool HasError(Expression expression)
		{
			return null != expression.Find(0, (_, e) => e is SqlErrorExpression);
		}

		public Expression ConvertExtension(DbFunc.ExpressionAttribute attr, IClauseContext context, Expression expr, ProjectFlags flags)
		{
			var rootContext     = context;
			var rootSelectQuery = context.SelectQuery;

			var root = GetRootContext(context.Parent, new ContextRefExpression(context.ElementType, context), true);
			if (root != null)
			{
				rootContext = root.BuildContext;
			}

			if (rootContext is GroupByContext groupBy)
			{
				rootSelectQuery = groupBy.SubQuery.SelectQuery;
			}

			var transformed = attr.GetExpression((builder: this, context: rootContext, flags),
				DBLive,
				this,
				rootSelectQuery, expr,
				static (context, e, descriptor, inline) =>
					context.builder.ConvertToExtensionSql(context.context, context.flags, e, descriptor, inline));

			if (transformed is SqlPlaceholderExpression placeholder)
			{
				RegisterExtensionAccessors(expr);

				placeholder = placeholder.WithSql(PosProcessCustomExpression(expr, placeholder.Sql, NullabilityContext.GetContext(placeholder.SelectQuery)));

				return placeholder.WithPath(expr);
			}

			if (attr.ServerSideOnly)
			{
				if (transformed is SqlErrorExpression errorExpr)
					return SqlErrorExpression.EnsureError(errorExpr, expr.Type);
				return SqlErrorExpression.EnsureError(expr, expr.Type);
			}

			return expr;
		}

		public Expression HandleExtension(IClauseContext context, Expression expr, ProjectFlags flags)
		{
			// Handling ExpressionAttribute
			//
			if (expr.NodeType == ExpressionType.Call || expr.NodeType == ExpressionType.MemberAccess)
			{
				MemberInfo memberInfo;
				if (expr.NodeType == ExpressionType.Call)
				{
					memberInfo = ((MethodCallExpression)expr).Method;
				}
				else
				{
					memberInfo = ((MemberExpression)expr).Member;
				}

				var attr = memberInfo.GetExpressionAttribute(DBLive);

				if (attr != null)
				{
					return ConvertExtension(attr, context, expr, flags);
				}
			}

			return expr;
		}

		public void RegisterExtensionAccessors(Expression expression)
		{
			void Register(Expression expr)
			{
				if (!expr.Type.IsScalar() && CanBeCompiled(expr, true))
					ParametersContext.ApplyAccessors(expr, true);

			}

			// Extensions may have instance reference. Try to register them as parameterized to disallow caching objects in Expression Tree
			//
			if (expression is MemberExpression { Expression: not null } me)
			{
				Register(me.Expression);
			}
			else if (expression is MethodCallExpression mc)
			{
				if (mc.Object != null)
				{
					Register(mc.Object);
				}

				var dependentParameters = SqlQueryDependentAttributeHelper.GetQueryDependentAttributes(mc.Method);
				for (var index = 0; index < mc.Arguments.Count; index++)
				{
					if (dependentParameters != null && dependentParameters[index] != null)
						continue;

					var arg = mc.Arguments[index];
					Register(arg);
				}
			}
		}

		public Expression FinalizeConstructors(IClauseContext context, Expression inputExpression, bool deduplicate)
		{
			using var finalizeVisitor = _finalizeVisitorPool.Allocate();
			var generator       = new ExpressionGenerator();

			// Runs SqlGenericConstructorExpression deduplication and generating actual initializers
			var expression = finalizeVisitor.Value.Finalize(inputExpression, context, generator);

			generator.AddExpression(expression);

			var result = generator.Build();
			return result;
		}

		sealed class SubQueryContextInfo
		{
			public Expression     SequenceExpression = null!;
			public string?        ErrorMessage;
			public IClauseContext? Context;
			public bool           IsSequence;
		}

		public Expression CorrectRoot(IClauseContext? currentContext, Expression expr)
		{
			if (expr is MethodCallExpression mc && mc.IsQueryable())
			{
				var firstArg = CorrectRoot(currentContext, mc.Arguments[0]);
				if (!ReferenceEquals(firstArg, mc.Arguments[0]))
				{
					var args = mc.Arguments.ToArray();
					args[0] = firstArg;
					return mc.Update(null, args);
				}
			}
			else if (expr is ContextRefExpression { BuildContext: DefaultIfEmptyContext di })
			{
				return CorrectRoot(di.Sequence, new ContextRefExpression(expr.Type, di.Sequence));
			}

			var newExpr = BuildProjection(currentContext, expr, ProjectFlags.Traverse);
			if (!ExpressionEqualityComparer.Instance.Equals(newExpr, expr))
			{
				newExpr = CorrectRoot(currentContext, newExpr);
			}

			return newExpr;
		}

		public ContextRefExpression? GetRootContext(IClauseContext? currentContext, Expression? expression, bool isAggregation)
		{
			if (expression == null)
				return null;

			if (expression is MemberExpression memberExpression)
			{
				expression = GetRootContext(currentContext, memberExpression.Expression, isAggregation);
			}
			if (expression is MethodCallExpression methodCallExpression && methodCallExpression.IsQueryable())
			{
				if (isAggregation)
					expression = GetRootContext(currentContext, methodCallExpression.Arguments[0], isAggregation);
			}
			else if (expression is ContextRefExpression)
			{
				var newExpression = BuildProjection(currentContext, expression, isAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root);

				if (!ExpressionEqualityComparer.Instance.Equals(newExpression, expression))
					expression = GetRootContext(currentContext, newExpression, isAggregation);
			}

			return expression as ContextRefExpression;
		}

		class SubqueryCacheKey
		{
			public SubqueryCacheKey(SelectQueryClause selectQuery, Expression expression)
			{
				SelectQuery = selectQuery;
				Expression  = expression;
			}

			public SelectQueryClause SelectQuery { get; }
			public Expression Expression { get; }

			sealed class BuildContextExpressionEqualityComparer : IEqualityComparer<SubqueryCacheKey>
			{
				public bool Equals(SubqueryCacheKey? x, SubqueryCacheKey? y)
				{
					if (ReferenceEquals(x, y))
					{
						return true;
					}

					if (ReferenceEquals(x, null))
					{
						return false;
					}

					if (ReferenceEquals(y, null))
					{
						return false;
					}

					if (x.GetType() != y.GetType())
					{
						return false;
					}

					return x.SelectQuery.Equals(y.SelectQuery, ExpressionWord.DefaultComparer) && ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression);
				}

				public int GetHashCode(SubqueryCacheKey obj)
				{
					unchecked
					{
						var hashCode = obj.SelectQuery.SourceID.GetHashCode();
						hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression);
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<SubqueryCacheKey> Comparer { get; } = new BuildContextExpressionEqualityComparer();
		}

		Dictionary<SubqueryCacheKey, SubQueryContextInfo>? _buildContextCache;
		Dictionary<SubqueryCacheKey, SubQueryContextInfo>? _testBuildContextCache;

		SubQueryContextInfo GetSubQueryContext(IClauseContext inContext, ref IClauseContext context, Expression expr, ProjectFlags flags)
		{
			context   = inContext;
			var testExpression = CorrectRoot(context, expr);
			var cacheKey       = new SubqueryCacheKey(context.SelectQuery, testExpression);

			var shouldCache = flags.IsSql() || flags.IsExpression() || flags.IsExtractProjection() || flags.IsRoot();

			if (shouldCache && _buildContextCache?.TryGetValue(cacheKey, out var item) == true)
				return item;

			if (flags.IsTest())
			{
				if (_testBuildContextCache?.TryGetValue(cacheKey, out var testItem) == true)
					return testItem;
			}

			var rootQuery = GetRootContext(context, testExpression, false);
			rootQuery ??= GetRootContext(context, expr, false);

			if (rootQuery != null)
				context = rootQuery.BuildContext;

			var ctx = GetSubQuery(context, testExpression, flags, out var isSequence, out var errorMessage);

			var info = new SubQueryContextInfo { SequenceExpression = testExpression, Context = ctx, IsSequence = isSequence, ErrorMessage = errorMessage};

			if (shouldCache)
			{
				if (flags.IsTest())
				{
					_testBuildContextCache           ??= new(SubqueryCacheKey.Comparer);
					_testBuildContextCache[cacheKey] =   info;
				}
				else
				{
					_buildContextCache           ??= new(SubqueryCacheKey.Comparer);
					_buildContextCache[cacheKey] =   info;
				}
			}

			return info;
		}

		public static bool IsSingleElementContext(IClauseContext context)
			=> context is FirstSingleContext;

		Expression TranslateDetails(IClauseContext context, Expression expr, ProjectFlags flags)
		{
			using var visitor = _buildVisitorPool.Allocate();
			return visitor.Value.Build(context, expr, flags, BuildFlags.ForceAssignments | BuildFlags.IgnoreRoot);
		}

		public Expression PrepareSubqueryExpression(Expression expr)
		{
			var newExpr = expr;

			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (mc.IsQueryable(_singleElementMethods))
				{
					if (mc.Arguments is [var a0, var a1])
					{
						Expression whereMethod;
						var typeArguments = mc.Method.GetGenericArguments();
						if (mc.Method.DeclaringType == typeof(Queryable))
						{
							var methodInfo = Methods.Queryable.Where.MakeGenericMethod(typeArguments);
							whereMethod = Expression.Call(methodInfo, a0, a1);
							newExpr = Expression.Call(typeof(Queryable), mc.Method.Name, typeArguments, whereMethod);
						}
						else
						{
							var methodInfo = Methods.Enumerable.Where.MakeGenericMethod(typeArguments);
							whereMethod = Expression.Call(methodInfo, a0, a1);
							newExpr = Expression.Call(typeof(Enumerable), mc.Method.Name, typeArguments, whereMethod);
						}
					}
				}
			}

			return newExpr;
		}

		public Expression? TryGetSubQueryExpression(IClauseContext context, Expression expr, string? alias, ProjectFlags flags, out bool isSequence, out Expression? corrected)
		{
			isSequence = false;
			corrected  = null;

			if (flags.IsTraverse())
				return null;

			var unwrapped = expr.Unwrap();

			if (unwrapped is SqlErrorExpression)
				return expr;

			if (unwrapped is BinaryExpression or ConditionalExpression or DefaultExpression or DefaultValueExpression or SqlDefaultIfEmptyExpression)
				return null;

			if (unwrapped is SqlGenericConstructorExpression or ConstantExpression or SqlEagerLoadExpression)
				return null;

			if (unwrapped is ContextRefExpression contextRef && contextRef.BuildContext.ElementType == expr.Type)
				return null;

			if (SequenceHelper.IsSpecialProperty(unwrapped, out _, out _))
				return null;

			if (!flags.IsSubquery())
			{
				if (CanBeCompiled(unwrapped, true))
					return null;

				if (unwrapped is MemberInitExpression or NewExpression or NewArrayExpression)
				{
					var withDetails = TranslateDetails(context, unwrapped, flags);
					if (CanBeCompiled(withDetails, true))
						return null;
				}
			}

			if (unwrapped is MemberExpression me)
			{
				var attr = me.Member.GetExpressionAttribute(DBLive);
				if (attr != null)
					return null;
			}

			var info = GetSubQueryContext(context, ref context, unwrapped, flags);
			isSequence = info.IsSequence;

			if (info.Context == null)
			{
				if (isSequence)
				{
					if (flags.IsExpression())
					{
						var prepared = PrepareSubqueryExpression(expr);
						if (!ReferenceEquals(prepared, expr))
							corrected = prepared;
						return null;
					}

					return new SqlErrorExpression(expr, info.ErrorMessage, expr.Type);
				}

				return null;
			}

			if (!IsSingleElementContext(info.Context) && expr.Type.IsEnumerableType(info.Context.ElementType) && !flags.IsExtractProjection())
			{
				var eager = (Expression)new SqlEagerLoadExpression(unwrapped);
				eager = SqlAdjustTypeExpression.AdjustType(eager, expr.Type, DBLive);
				return eager;
			}

			return new ContextRefExpression(unwrapped.Type, info.Context);
		}




		static string [] _singleElementMethods =
		{
			nameof(Enumerable.FirstOrDefault),
			nameof(Enumerable.First),
			nameof(Enumerable.Single),
			nameof(Enumerable.SingleOrDefault),
		};



		#endregion

		#region PreferServerSide

		private FindVisitor<ClauseSqlTranslator>? _enforceServerSideVisitorTrue;
		private FindVisitor<ClauseSqlTranslator>? _enforceServerSideVisitorFalse;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private FindVisitor<ClauseSqlTranslator> GetVisitor(bool enforceServerSide)
		{
			if (enforceServerSide)
				return _enforceServerSideVisitorTrue ??= FindVisitor<ClauseSqlTranslator>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, true));
			else
				return _enforceServerSideVisitorFalse ??= FindVisitor<ClauseSqlTranslator>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, false));
		}

		public bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			if (expr.Type == typeof(DbFunc.SqlID))
				return true;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
				{
					var pi = (MemberExpression)expr;
					var l  = Expressions.ConvertMember(DBLive, pi.Expression?.Type, pi.Member);

					if (l != null)
					{
						var info = l.Body.Unwrap();

						if (l.Parameters.Count == 1 && pi.Expression != null)
							info = info.Replace(l.Parameters[0], pi.Expression);

						return GetVisitor(enforceServerSide).Find(info) != null;
					}

					
					return  enforceServerSide && !CanBeCompiled(expr, false);
				}

				case ExpressionType.Call:
				{
					var pi = (MethodCallExpression)expr;
					var l  = Expressions.ConvertMember(DBLive, pi.Object?.Type, pi.Method);

					if (l != null)
						return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

					var registryEntry = DBLive.dialect.dbFuncRegistry.Resolve(pi.Method);
					if (registryEntry != null)
						return registryEntry.PreferServerSide || enforceServerSide;

					var attr = pi.Method.GetExpressionAttribute(DBLive);
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr, false);
				}

				default:
				{
					if (expr is BinaryExpression binary)
					{
						var l = Expressions.ConvertBinary(DBLive, binary);
						if (l != null)
						{
							var body = l.Body.Unwrap();
							var newExpr = body.Transform((l, binary), static (context, wpi) =>
							{
								if (wpi.NodeType == ExpressionType.Parameter)
								{
									if (context.l.Parameters[0] == wpi)
										return context.binary.Left;
									if (context.l.Parameters[1] == wpi)
										return context.binary.Right;
								}

								return wpi;
							});

							return PreferServerSide(newExpr, enforceServerSide);
						}
					}
					break;
				}
			}

			return false;
		}

		#endregion

		#region BuildMultipleQuery

		public Expression? AssociationRoot;
		public Stack<Tuple<AccessorMember, IClauseContext, List<IncludeInfo[]>?>>? AssociationPath;

		#endregion

	}
}
