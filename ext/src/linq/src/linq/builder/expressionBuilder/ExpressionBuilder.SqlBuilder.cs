using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Common.Internal;
	using Data;
	using Extensions;
	using Translation;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using DataProvider;
	using mooSQL.data.model;
	using mooSQL.data;
	using mooSQL.data.model.affirms;
    using mooSQL.data.Mapping;
    using mooSQL.utils;
    using mooSQL.data.mapping;

    partial class ExpressionBuilder
	{
		#region LinqOptions shortcuts

		public bool CompareNullsAsValues => DBLive.dialect.Option.CompareNullsAsValues;

		#endregion

		#region Build Where

		public IBuildContext? BuildWhere(
			IBuildContext?   parent,
			IBuildContext    sequence,
			LambdaExpression condition,
			bool             checkForSubQuery,
			bool             enforceHaving,
			bool             isTest)
		{
			var buildSequnce = sequence;

			if (enforceHaving)
			{
				var root = sequence.Builder.GetRootContext(sequence,
					new ContextRefExpression(sequence.ElementType, sequence), true);

				if (root != null && root.BuildContext is GroupByBuilder.GroupByContext groupByContext)
				{
					buildSequnce = groupByContext.SubQuery;
				}
				else
				{
					enforceHaving = false;
				}
			}

			if (!enforceHaving)
			{
				if (buildSequnce is not SubQueryContext subQuery || subQuery.NeedsSubqueryForComparison)
				{
					buildSequnce = new SubQueryContext(sequence);
				}

				sequence.SetAlias(condition.Parameters[0].Name);
				sequence = buildSequnce;
			}

			var body = SequenceHelper.PrepareBody(condition, sequence);
			var expr = body.Unwrap();

			var sc = new SearchConditionWord();

			var flags = ProjectFlags.SQL;
			if (isTest)
				flags |= ProjectFlags.Test;

			if (!BuildSearchCondition(buildSequnce, expr, flags, sc, out var error))
			{
				if (parent == null && !isTest)
					throw error.CreateException();
				return null;
			}

			if (!isTest)
			{
				if (enforceHaving)
					buildSequnce.SelectQuery.Having.ConcatSearchCondition(sc);
				else
					buildSequnce.SelectQuery.Where.ConcatSearchCondition(sc);
			}

			if (!enforceHaving)
			{
				return buildSequnce;
			}

			return sequence;
		}

		#endregion

		#region Build Skip/Take

		public void BuildTake(IBuildContext sequence, IExpWord expr, TakeHintType? hints)
		{
			var sql = sequence.SelectQuery;

			if (hints != null && !DBLive.dialect.Option.ProviderFlags.GetIsTakeHintsSupported(hints.Value))
				throw new LinqException($"TakeHints are {hints} not supported by current database");

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqException("Take with hints could not be applied with Skip");

			if (sql.Select.TakeValue != null)
			{
				expr = new ConditionWord(
					new ExprExpr(sql.Select.TakeValue, AffirmWord.Operator.Less, expr, null),
					sql.Select.TakeValue,
					expr);
			}

			sql.Select.Take(expr, hints);
		}

		public void BuildSkip(IBuildContext sequence, IExpWord expr)
		{
			var sql = sequence.SelectQuery;

			if (sql.Select.TakeHints != null)
				throw new LinqException("Skip could not be applied with Take with hints");

			if (sql.Select.SkipValue != null)
				sql.Select.Skip(new BinaryWord(typeof(int), sql.Select.SkipValue, "+", expr, PrecedenceLv.Additive));
			else
				sql.Select.Skip(expr);

			if (sql.Select.TakeValue != null)
			{
				sql.Select.Take(
					new BinaryWord(typeof(int), sql.Select.TakeValue, "-", expr, PrecedenceLv.Additive),
					sql.Select.TakeHints);
			}
		}

		#endregion

		#region SubQueryToSql

		/// <summary>
		/// Checks that provider can handle limitation inside subquery. This function is tightly coupled with <see cref="SelectQueryOptimizerVisitor.OptimizeApply"/>
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public bool IsSupportedSubquery(IBuildContext parent, IBuildContext context, out string? errorMessage)
		{
			errorMessage = null;

			if (!_validateSubqueries)
				return true;

			// No check during recursion. Cloning may fail
			if (parent.Builder.IsRecursiveBuild)
				return true;

			if (!context.Builder.DBLive.dialect.Option.ProviderFlags.IsApplyJoinSupported)
			{
				// We are trying to simulate what will be with query after optimizer's work
				//
				var cloningContext = new CloningContext();

				var clonedParentContext = cloningContext.CloneContext(parent);
				var clonedContext       = cloningContext.CloneContext(context);

				cloningContext.UpdateContextParents();

				var expr = parent.Builder.MakeExpression(clonedContext, new ContextRefExpression(clonedContext.ElementType, clonedContext), ProjectFlags.SQL);

				expr = parent.Builder.ToColumns(clonedParentContext, expr);

                JoinTableWord? fakeJoin = null;

				// add fake join there is no still reference
				if (null == clonedParentContext.SelectQuery.Find(e => e is SelectQueryClause sc && sc == clonedContext.SelectQuery))
				{
					//fakeJoin = clonedContext.SelectQuery.OuterApply().JoinedTable;

					//clonedParentContext.SelectQuery.From.Tables[0].Joins.Add(fakeJoin);
					clonedParentContext.SelectQuery.From.OuterApply(clonedContext.SelectQuery, "", null);

                }

				using var visitor = QueryHelper.SelectOptimizer.Allocate();

#if DEBUG

				var sqlText = clonedParentContext.SelectQuery.ToDebugString();

#endif

				var optimizedQuery = (SelectQueryClause)visitor.Value.Optimize(
					root: clonedParentContext.SelectQuery,
					rootElement: clonedParentContext.SelectQuery,
					providerFlags: parent.Builder.DBLive.dialect.Option.ProviderFlags,
					removeWeakJoins: false,
					DBLive,
					evaluationContext: new EvaluateContext()
				);

				if (!SqlProviderHelper.IsValidQuery(optimizedQuery, 
					    parentQuery: null, 
					    fakeJoin: fakeJoin, 
					    forColumn: false, 
					    parent.Builder.DBLive.dialect.Option.ProviderFlags, 
					    out errorMessage))
				{
					return false;
				}
			}

			return true;
		}

		int _gettingSubquery;


		#endregion

		#region ConvertExpression

		public Expression ConvertExpression(Expression expression)
		{
			using var visitor = _exposeVisitorPool.Allocate();

			var result = visitor.Value.ExposeExpression(DBLive, _optimizationContext, ParameterValues, expression, includeConvert : true, optimizeConditions : false, compactBinary : false);

			return result;
		}

		public Expression ConvertSingleExpression(Expression expression, bool inProjection)
		{
			// We can convert only these expressions, so it is shortcut to do not allocate visitor

			if (expression.NodeType is ExpressionType.Call
				                    or ExpressionType.MemberAccess
				                    or ExpressionType.New
				|| expression is BinaryExpression)
			{
				var result = ConvertExpression(expression);

				return result;
			}

			return expression;
		}

		#endregion

		#region BuildExpression

		public Expression ConvertToExtensionSql(IBuildContext context, ProjectFlags flags, Expression expression, EntityColumn? columnDescriptor, bool? inlineParameters)
		{

			try
			{


				expression = expression.UnwrapConvertToObject();
				var unwrapped = expression.Unwrap();

				if (unwrapped is LambdaExpression lambda)
				{
					var contextRefExpression = new ContextRefExpression(lambda.Parameters[0].Type, context);

					var body = lambda.GetBody(contextRefExpression);

					return ConvertToSqlExpr(context, body, flags : flags.SqlFlag() | ProjectFlags.ForExtension, columnDescriptor : columnDescriptor);
				}

				if (unwrapped is ContextRefExpression contextRef)
				{
					contextRef = contextRef.WithType(contextRef.BuildContext.ElementType);

					var result = ConvertToSqlExpr(contextRef.BuildContext, contextRef,
						flags : flags.SqlFlag() | ProjectFlags.ForExtension, columnDescriptor : columnDescriptor);

					if (result is SqlPlaceholderExpression)
					{
						if (result.Type != expression.Type)
						{
							result = Expression.Convert(result, expression.Type);
							result = ConvertToSqlExpr(contextRef.BuildContext, result, flags: flags.SqlFlag() | ProjectFlags.ForExtension, columnDescriptor: columnDescriptor);
						}

						result = UpdateNesting(context, result);

						return result;
					}
				}
				else
				{
					var converted = ConvertToSqlExpr(context, expression, flags : flags.SqlFlag() | ProjectFlags.ForExtension, columnDescriptor : columnDescriptor);

					if (converted is SqlPlaceholderExpression or SqlErrorExpression)
					{
						return converted;
					}

					// Weird case, see Stuff2 test
					if (!CanBeCompiled(expression, false))
					{
						var buildResult = TryBuildSequence(new BuildInfo(context, expression, new SelectQueryClause()));
						if (buildResult.BuildContext != null)
						{
							unwrapped = new ContextRefExpression(buildResult.BuildContext.ElementType, buildResult.BuildContext);
							var result = ConvertToSqlExpr(buildResult.BuildContext, unwrapped,
								flags : flags.SqlFlag() | ProjectFlags.ForExtension, columnDescriptor : columnDescriptor);

							if (result is SqlPlaceholderExpression { SelectQuery: not null } placeholder)
							{
								_ = ToColumns(placeholder.SelectQuery, placeholder);

								return CreatePlaceholder(context, placeholder.SelectQuery, unwrapped);
							}
						}
					}
				}

				return expression;
			}
			finally
			{

			}
		}

		[DebuggerDisplay("S: {SelectQuery?.SourceID} F: {Flags}, E: {Expression}, C: {Context}")]
		readonly struct SqlCacheKey
		{
			public SqlCacheKey(Expression? expression, IBuildContext? context, EntityColumn? columnDescriptor, SelectQueryClause? selectQuery, ProjectFlags flags)
			{
				Expression       = expression;
				Context          = context;
				ColumnDescriptor = columnDescriptor;
				SelectQuery      = selectQuery;
				Flags            = flags;
			}

			public Expression?       Expression       { get; }
			public IBuildContext?    Context          { get; }
			public EntityColumn? ColumnDescriptor { get; }
			public SelectQueryClause?      SelectQuery      { get; }
			public ProjectFlags      Flags            { get; }

			private sealed class SqlCacheKeyEqualityComparer : IEqualityComparer<SqlCacheKey>
			{
				public bool Equals(SqlCacheKey x, SqlCacheKey y)
				{
					return ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression) &&
						   Equals(x.Context, y.Context)                                           &&
						   Equals(x.SelectQuery, y.SelectQuery)                                   &&
						   Equals(x.ColumnDescriptor, y.ColumnDescriptor)                         &&
						   x.Flags == y.Flags;
				}

				public int GetHashCode(SqlCacheKey obj)
				{
					unchecked
					{
						var hashCode = (obj.Expression != null ? ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression) : 0);
						hashCode = (hashCode * 397) ^ (obj.Context          != null ? obj.Context.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (obj.SelectQuery      != null ? obj.SelectQuery.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (obj.ColumnDescriptor != null ? obj.ColumnDescriptor.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (int)obj.Flags;
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<SqlCacheKey> SqlCacheKeyComparer { get; } = new SqlCacheKeyEqualityComparer();
		}

		[DebuggerDisplay("S: {SelectQuery?.SourceID}, E: {Expression}")]
		readonly struct ColumnCacheKey
		{
			public ColumnCacheKey(Expression? expression, Type resultType, SelectQueryClause selectQuery, SelectQueryClause? parentQuery)
			{
				Expression  = expression;
				ResultType  = resultType;
				SelectQuery = selectQuery;
				ParentQuery = parentQuery;
			}

			public Expression?  Expression  { get; }
			public Type         ResultType  { get; }
			public SelectQueryClause  SelectQuery { get; }
			public SelectQueryClause? ParentQuery { get; }

			private sealed class ColumnCacheKeyEqualityComparer : IEqualityComparer<ColumnCacheKey>
			{
				public bool Equals(ColumnCacheKey x, ColumnCacheKey y)
				{
					return x.ResultType == y.ResultType                                           &&
						   ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression) &&
						   ReferenceEquals(x.SelectQuery, y.SelectQuery) &&
						   ReferenceEquals(x.ParentQuery, y.ParentQuery);
				}

				public int GetHashCode(ColumnCacheKey obj)
				{
					unchecked
					{
						var hashCode = obj.ResultType.GetHashCode();
						hashCode = (hashCode * 397) ^ (obj.Expression != null ? ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression) : 0);
						hashCode = (hashCode * 397) ^ obj.SelectQuery?.GetHashCode() ?? 0;
						hashCode = (hashCode * 397) ^ (obj.ParentQuery != null ? obj.ParentQuery.GetHashCode() : 0);
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<ColumnCacheKey> ColumnCacheKeyComparer { get; } = new ColumnCacheKeyEqualityComparer();
		}

		Dictionary<SqlCacheKey, Expression> _cachedSql        = new(SqlCacheKey.SqlCacheKeyComparer);

		public SqlPlaceholderExpression ConvertToSqlPlaceholder(IBuildContext? context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, bool forceParameter = false)
		{
			var expr = ConvertToSqlExpr(context, expression, flags, unwrap, columnDescriptor,
				isPureExpression : isPureExpression, forceParameter : forceParameter);

			if (expr is not SqlPlaceholderExpression placeholder)
			{
				if (expr is SqlErrorExpression errorExpression)
					throw errorExpression.CreateException();

				throw CreateSqlError(context, expression).CreateException();
			}

			return placeholder;
		}

		public IExpWord ConvertToSql(IBuildContext? context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, bool forceParameter = false)
		{
			var placeholder = ConvertToSqlPlaceholder(context, expression, flags, unwrap : unwrap,
				columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, forExtension : forExtension,
				forceParameter : forceParameter);

			return placeholder.Sql;
		}
        public IExpWord ConvertToSqlEn(IBuildContext? context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, bool forceParameter = false)
        {
            var placeholder = ConvertToSqlPlaceholder(context, expression, flags, unwrap: unwrap,
                columnDescriptor: null, isPureExpression: isPureExpression, forExtension: forExtension,
                forceParameter: forceParameter);

            return placeholder.Sql;
        }

        public static SqlPlaceholderExpression CreatePlaceholder(IBuildContext? context, IExpWord sqlExpression,
			Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			var placeholder = new SqlPlaceholderExpression(context?.SelectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
			return placeholder;
		}

		public static SqlPlaceholderExpression CreatePlaceholder(SelectQueryClause? selectQuery, IExpWord sqlExpression,
			Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			var placeholder = new SqlPlaceholderExpression(selectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
			return placeholder;
		}

		/// <summary>
		/// Converts to Expression which may contain SQL or convert error.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="expression"></param>
		/// <param name="flags"></param>
		/// <param name="unwrap"></param>
		/// <param name="columnDescriptor"></param>
		/// <param name="isPureExpression"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public Expression ConvertToSqlExpr(IBuildContext? context, Expression expression,
			ProjectFlags flags = ProjectFlags.SQL,
			bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forceParameter = false,
			string? alias = null)
		{
			if (expression is SqlPlaceholderExpression)
				return expression;

			// remove keys flag. We can cache SQL
			var cacheFlags = flags & ~ProjectFlags.Keys;

			var forExtension = flags.IsForExtension();
			flags &= ~ProjectFlags.ForExtension;

			var cacheKey = new SqlCacheKey(expression, null, columnDescriptor, context?.SelectQuery, cacheFlags);

			var cache = expression is SqlPlaceholderExpression ||
						(null != expression.Find(1, (_, e) => e is ContextRefExpression));

			if (cache && _cachedSql.TryGetValue(cacheKey, out var sqlExpr))
			{
				if (sqlExpr is SqlPlaceholderExpression cachedPlaceholder)
					return cachedPlaceholder.WithTrackingPath(expression);
				return sqlExpr;
			}

            IExpWord? sql              = null;
			Expression?     result           = null;

			var newExpr = expression;

			newExpr = MakeExpression(context, newExpr, flags);

			if (newExpr is SqlErrorExpression)
				return newExpr;

			var noConvert = newExpr.UnwrapConvert();
			if (typeof(IExpWord).IsSameOrParentOf(newExpr.Type) || typeof(IExpWord).IsSameOrParentOf(noConvert.Type))
			{
				var valid = true;
				if (newExpr is MethodCallExpression mc)
				{
					var type = mc.Object?.Type ?? mc.Method.DeclaringType;
					//if (type != null && MappingSchema.HasAttribute<Sql.ExpressionAttribute>(type, mc.Method))
					//	valid = false;
				}
				else if (newExpr is MemberExpression me)
				{
					var type = me.Expression?.Type ?? me.Member.DeclaringType;
					//if (type != null && MappingSchema.HasAttribute<Sql.ExpressionAttribute>(type, me.Member))
					//	valid = false;
				}

				if (valid)
					sql = ConvertToInlinedSqlExpression(context, newExpr);
			}
			else if (typeof(IToSqlConverter).IsSameOrParentOf(newExpr.Type) || typeof(IToSqlConverter).IsSameOrParentOf(noConvert.Type))
			{
				sql = ConvertToSqlConvertible(context, newExpr);
			}

			if (sql == null && !flags.IsExpression())
			{
				if (!PreferServerSide(newExpr, false))
				{
					//if (columnDescriptor?.ValueConverter == null && CanBeConstant(newExpr) && !forceParameter)
					//{
					//	sql = BuildConstant(context?.Builder.DBLive, newExpr, columnDescriptor);
					//}

					if (sql == null && CanBeCompiled(newExpr, flags.IsExpression()))
					{
						if (!TryTranslateMember(newExpr, out result))
						{
							if (flags.IsKeys())
								newExpr = ParseGenericConstructor(newExpr, flags, columnDescriptor);

							if (newExpr is not SqlGenericConstructorExpression)
							{
								sql = ParametersContext.BuildParameter(context, newExpr, columnDescriptor, forceNew : forceParameter, alias : alias)?.SqlParameter;
							}
						}
					}
				}
			}

			if (sql == null)
			{
				if (newExpr is SqlPlaceholderExpression)
				{
					result = newExpr;
				}
			}

			if (result == null && context != null && forExtension && newExpr is SqlGenericConstructorExpression)
			{
				var fullyTranslated = BuildSqlExpression(context, expression, flags, buildFlags : BuildFlags.ForceAssignments);
				fullyTranslated = UpdateNesting(context, fullyTranslated);

				var placeholders = CollectDistinctPlaceholders(fullyTranslated);

				var usedSources = new HashSet<ITableNode>();

				foreach(var p in placeholders)
					QueryHelper.GetUsedSources(p.Sql, usedSources);

				if (usedSources.Count == 1)
				{
					var ts = usedSources.First();
					sql = ts.All;
				}
			}

			if (result == null)
			{
				if (sql != null)
				{
					result = CreatePlaceholder(context?.SelectQuery, sql, newExpr, alias: alias);
				}
				else
				{
					newExpr = ConvertSingleExpression(newExpr, flags.IsExpression());
					
					if (!TryTranslateMember(newExpr, out result))
						result  = ConvertToSqlInternal(context, newExpr, flags, unwrap: unwrap, columnDescriptor: columnDescriptor, isPureExpression: isPureExpression, alias: alias);
				}
			}

			// nesting for Expressions updated in finalization
			var updateNesting = !flags.IsTest();

			if (updateNesting && context != null)
			{
				result = UpdateNesting(context, result);
			}

			if (result is SqlPlaceholderExpression placeholder)
			{
				if (expression is not SqlPlaceholderExpression)
					placeholder = placeholder.WithTrackingPath(expression);

				if (alias != null)
					placeholder = placeholder.WithAlias(alias);

				if (cache)
				{
					if (updateNesting && placeholder.SelectQuery != context?.SelectQuery &&
						placeholder.Sql is not ColumnWord)
					{
						// recreate placeholder
						placeholder = CreatePlaceholder(context?.SelectQuery, placeholder.Sql, placeholder.Path,
							placeholder.ConvertType, placeholder.Alias, placeholder.Index, trackingPath: placeholder.TrackingPath);
					}

					if (expression is not SqlPlaceholderExpression)
						placeholder = placeholder.WithTrackingPath(expression);

					_cachedSql[cacheKey] = placeholder;
				}

				result = placeholder;
			}

			return result;

			bool TryTranslateMember(Expression toTranslate, [NotNullWhen(true)] out Expression? translationResult)
			{
				var translated = TranslateMember(context, flags, columnDescriptor, alias, toTranslate);
				if (translated != null)
				{
					if (translated is SqlPlaceholderExpression)
						translationResult = translated;
					else if (!SequenceHelper.IsSqlReady(translated))
					{
						var sqLError = translated.Find(1, (_, e) => e is SqlErrorExpression);
						if (sqLError != null)
							translationResult = ((SqlErrorExpression)sqLError).WithType(expression.Type);
						else
						{
							translationResult = null;
							return false;
						}
					}

					translationResult = ConvertToSqlExpr(context, translated, flags, unwrap, columnDescriptor, isPureExpression, forceParameter, alias);
					return true;
				}

				translationResult = null;
				return false;
			}
		}

		internal bool IsForceParameter(Expression expression, EntityColumn? columnDescriptor)
		{
			//if (columnDescriptor?.ValueConverter != null)
			//{
			//	return true;
			//}

			var converter = TypeConverterUtil.GetConvertType(DBLive,expression.Type, typeof(DataParameter));
			if (converter != null)
			{
				return true;
			}

			return false;
		}

		static TranslationFlags GetTranslationFlags(ProjectFlags flags)
		{
			var result = TranslationFlags.None;

			if (flags.IsSql())
				result |= TranslationFlags.Sql;

			if (flags.IsExpression())
				result |= TranslationFlags.Expression;

			return result;
		}

		Expression ConvertToSqlInternal(IBuildContext? context, Expression expression, ProjectFlags flags, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, string? alias = null)
		{
			if (unwrap)
				expression = expression.Unwrap();

			switch (expression.NodeType)
			{
				case ExpressionType.AndAlso:
				case ExpressionType.OrElse:
				case ExpressionType.Not:
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					var condition = new SearchConditionWord();
					if (!BuildSearchCondition(context, expression, flags, condition, out var error))
						return error.WithType(expression.Type);
					return CreatePlaceholder(context, condition, expression, alias : alias);
				}

				case ExpressionType.And:
				case ExpressionType.Or:
				{
					if (expression.Type == typeof(bool))
						goto case ExpressionType.AndAlso;
					goto case ExpressionType.Add;
				}

				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Divide:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Power:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Coalesce:
				{
					var e = (BinaryExpression)expression;

					var left  = e.Left;
					var right = e.Right;

					var shouldCheckColumn = e.Left.Type.UnwrapNullable() == e.Right.Type.UnwrapNullable();

					if (shouldCheckColumn)
					{
						right = right.Unwrap();
					}
					else
					{
						left  = left.Unwrap();
						right = right.Unwrap();
					}

					columnDescriptor = null;
					switch (expression.NodeType)
					{
						case ExpressionType.Add:
						case ExpressionType.AddChecked:
						case ExpressionType.And:
						case ExpressionType.Divide:
						case ExpressionType.ExclusiveOr:
						case ExpressionType.Modulo:
						case ExpressionType.Multiply:
						case ExpressionType.MultiplyChecked:
						case ExpressionType.Or:
						case ExpressionType.Power:
						case ExpressionType.Subtract:
						case ExpressionType.SubtractChecked:
						case ExpressionType.Coalesce:
						{
							columnDescriptor = SuggestColumnDescriptor(context, left, flags);
							break;
						}
						case ExpressionType.Equal:
						case ExpressionType.NotEqual:
						case ExpressionType.GreaterThan:
						case ExpressionType.GreaterThanOrEqual:
						case ExpressionType.LessThan:
						case ExpressionType.LessThanOrEqual:
						{
							columnDescriptor = SuggestColumnDescriptor(context, left, right, flags);
							break;
						}
					}

					if (left.Type != right.Type)
					{
						if (left.Type.UnwrapNullable() != right.Type.UnwrapNullable())
							columnDescriptor = null;
					}

					var leftExpr  = ConvertToSqlExpr(context, left,  flags.TestFlag(), columnDescriptor : columnDescriptor, isPureExpression : isPureExpression);
					var rightExpr = ConvertToSqlExpr(context, right, flags.TestFlag(), columnDescriptor : columnDescriptor, isPureExpression : isPureExpression);

					if (leftExpr is SqlErrorExpression errorLeft)
						return errorLeft.WithType(e.Type);

					if (rightExpr is SqlErrorExpression errorRight)
						return errorRight.WithType(e.Type);

					if (leftExpr is not SqlPlaceholderExpression || rightExpr is not SqlPlaceholderExpression)
						return e;

					var leftPlaceholder  = ConvertToSqlExpr(context, left,  flags, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression) as SqlPlaceholderExpression;
					if (leftPlaceholder == null)
						return e;
					var rightPlaceholder = ConvertToSqlExpr(context, right, flags, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression) as SqlPlaceholderExpression;
					if (rightPlaceholder == null)
						return e;

					var l = leftPlaceholder.Sql;
					var r = rightPlaceholder.Sql;
					var t = e.Type;

					switch (expression.NodeType)
					{
						case ExpressionType.Add:
						case ExpressionType.AddChecked: return CreatePlaceholder(context, new BinaryWord(t, l, "+", r, PrecedenceLv.Additive), expression, alias : alias);
						case ExpressionType.And: return CreatePlaceholder(context, new BinaryWord(t, l, "&", r,	PrecedenceLv.Bitwise), expression, alias : alias);
						case ExpressionType.Divide: return CreatePlaceholder(context, new BinaryWord(t, l, "/", r, PrecedenceLv.Multiplicative), expression, alias : alias);
						case ExpressionType.ExclusiveOr: return CreatePlaceholder(context, new BinaryWord(t, l, "^", r, PrecedenceLv.Bitwise), expression, alias : alias);
						case ExpressionType.Modulo: return CreatePlaceholder(context, new BinaryWord(t, l, "%", r, PrecedenceLv.Multiplicative), expression, alias : alias);
						case ExpressionType.Multiply:
						case ExpressionType.MultiplyChecked: return CreatePlaceholder(context, new BinaryWord(t, l, "*", r, PrecedenceLv.Multiplicative), expression, alias : alias);
						case ExpressionType.Or: return CreatePlaceholder(context, new BinaryWord(t, l, "|", r, PrecedenceLv.Bitwise), expression, alias : alias);
						case ExpressionType.Power: return CreatePlaceholder(context, new FunctionWord(t, "Power", l, r), expression, alias : alias);
						case ExpressionType.Subtract:
						case ExpressionType.SubtractChecked: return CreatePlaceholder(context, new BinaryWord(t, l, "-", r, PrecedenceLv.Subtraction), expression, alias : alias);
						case ExpressionType.Coalesce:
						{
							if (QueryHelper.UnwrapExpression(r, checkNullability: true) is FunctionWord c)
							{
								if (c.Name is "Coalesce" or PseudoFunctions.COALESCE)
								{
									var parms = new IExpWord[c.Parameters.Length + 1];

									parms[0] = l;
									c.Parameters.CopyTo(parms, 1);

									return CreatePlaceholder(context, PseudoFunctions.MakeCoalesce(t, parms), expression, alias : alias);
								}
							}

							return CreatePlaceholder(context, PseudoFunctions.MakeCoalesce(t, l, r), expression, alias : alias);
						}
					}

					break;
				}

				case ExpressionType.UnaryPlus:
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				{
					var e = (UnaryExpression)expression;
					var o = ConvertToSql(context, e.Operand);
					var t = e.Type;

					switch (expression.NodeType)
					{
						case ExpressionType.UnaryPlus: return CreatePlaceholder(context, o, expression);
						case ExpressionType.Negate:
						case ExpressionType.NegateChecked:
							return CreatePlaceholder(context, new BinaryWord(t, new ValueWord(-1), "*", o, PrecedenceLv.Multiplicative), expression, alias : alias);
					}

					break;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var e = (UnaryExpression)expression;

					if (!flags.IsTest() && context != null)
					{
						e = e.Update(UpdateNesting(context, e.Operand));
					}

					var operandExpr = ConvertToSqlExpr(context, e.Operand, flags, unwrap : unwrap, columnDescriptor : columnDescriptor);

					if (!SequenceHelper.IsSqlReady(operandExpr))
						return e;

					var placeholders = CollectDistinctPlaceholders(operandExpr);

					if (placeholders.Count == 1)
					{
						var placeholder = placeholders[0].WithType(expression.Type).WithPath(expression);

						if (e.Method == null && (e.IsLifted || e.Type == typeof(object)))
							return placeholder;

						if (e.Method == null && operandExpr is not SqlPlaceholderExpression)
							return e;

						if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
							return placeholder;

						if (e.Type == typeof(Enum) && e.Operand.Type.IsEnum)
							return placeholder;

						var t = e.Operand.Type;
						var s = DBLive.dialect.mapping.GetDbDataType(t);

						if (placeholder.Sql.SystemType != null && s.SystemType == typeof(object))
						{
							t = placeholder.Sql.SystemType;
							s = DBLive.dialect.mapping.GetDbDataType(t);
						}

						if (e.Type == t                                               ||
							t.IsEnum      && Enum.GetUnderlyingType(t)      == e.Type ||
							e.Type.IsEnum && Enum.GetUnderlyingType(e.Type) == t)
						{
							return placeholder;
						}

						return CreatePlaceholder(placeholder.SelectQuery,
							PseudoFunctions.MakeCast(placeholder.Sql,DBLive.dialect.mapping.GetDbDataType(e.Type)), expression,
							alias : alias);
					}

					return e;
				}

				case ExpressionType.Conditional:
				{
					var e = (ConditionalExpression)expression;

					var testExpr  = ConvertToSqlExpr(context, e.Test,    flags.TestFlag(), columnDescriptor: null, isPureExpression: isPureExpression);
					var trueExpr  = ConvertToSqlExpr(context, e.IfTrue,  flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);
					var falseExpr = ConvertToSqlExpr(context, e.IfFalse, flags.TestFlag(), columnDescriptor: columnDescriptor, isPureExpression: isPureExpression);

					if (testExpr is SqlPlaceholderExpression &&
						trueExpr is SqlPlaceholderExpression &&
						falseExpr is SqlPlaceholderExpression)
					{
						var testPredicate  = ConvertPredicate(context, e.Test, flags, out var error);

						if (testPredicate is null)
						{
							return SqlErrorExpression.EnsureError(error ?? expression, e.Type);
						}

						var trueSql  = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.IfTrue,  flags | ProjectFlags.ForceOuterAssociation, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression);
						var falseSql = (SqlPlaceholderExpression)ConvertToSqlExpr(context, e.IfFalse, flags | ProjectFlags.ForceOuterAssociation, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression);

						return CreatePlaceholder(context, new ConditionWord(testPredicate, trueSql.Sql, falseSql.Sql), expression, alias: alias);

					}

					return e;
				}

				case ExpressionType.Extension:
				{
					if (expression is SqlPlaceholderExpression)
					{
						return expression;
					}

					if (context != null && expression is SqlGenericConstructorExpression)
					{
						var result = BuildSqlExpression(context, expression, flags, buildFlags: BuildFlags.ForceAssignments);
						return result;
					}

					break;
				}

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)expression;

					var expr = ConvertExpression(expression);

					if (!ReferenceEquals(expr, expression))
					{
						return ConvertToSqlExpr(context, expr, flags : flags, unwrap : unwrap, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, alias : alias);
					}

					var attr = mc.Method.GetExpressionAttribute(DBLive);

					if (attr != null)
					{
						// Otherwise should be handled by MakeExpression
						if (IsSequence(context, mc))
						{
							if (attr.ServerSideOnly)
								return SqlErrorExpression.EnsureError(mc, mc.Type);
							break;
						}

						return ConvertExtensionToSql(context!, flags, attr, mc, checkAggregateRoot: true);
					}

					if (mc.Method.IsSqlPropertyMethodEx())
						return CreatePlaceholder(context, ConvertToSql(context, ConvertExpression(expression), unwrap: unwrap), expression, alias : alias);

					if (mc.Method.DeclaringType == typeof(string) && mc.Method.Name == "Format")
					{
						var sqlExpression = TryConvertFormatToSql(context, mc, isPureExpression, flags);
						if (sqlExpression != null)
							return CreatePlaceholder(context, sqlExpression, expression, alias : alias);
						break;
					}

					if (mc.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
					{
						var sqlExpr = ConvertToSqlExpr(context, mc.Arguments[0], flags : flags,
							unwrap : unwrap, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, alias : alias);

						if (sqlExpr is SqlPlaceholderExpression placeholderExpression)
						{
							sqlExpr = placeholderExpression.WithAlias(alias);
						}
						return sqlExpr;
					}

					if (context != null)
					{
						var newExpr = HandleExtension(context, mc, flags);
						expression = newExpr;
					}

					break;
				}

				case ExpressionType.MemberAccess:
				{
					if (context != null)
					{
						var handled = HandleExtension(context, expression, flags);

						if (!ExpressionEqualityComparer.Instance.Equals(handled, expression))
						{
							expression = handled;
							break;
						}
					}

					var exposed = ConvertSingleExpression(expression, false);

					if (!ReferenceEquals(exposed, expression))
					{
						var newExpr = ConvertToSqlExpr(context, exposed, flags : flags, unwrap : unwrap,
							columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, alias : alias);

						if (newExpr is SqlPlaceholderExpression placeholder)
							newExpr = placeholder.WithPath(expression);

						expression = newExpr;
					}

					var memberExpression = (MemberExpression)expression;
					if (memberExpression.Member.IsNullableHasValueMember())
					{
						var converted = ConvertToSqlExpr(context, Expression.NotEqual(memberExpression.Expression!, Expression.Constant(null, memberExpression.Expression!.Type)), 
							flags : flags, unwrap : unwrap, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, alias : alias);

						if (converted is SqlPlaceholderExpression placeholder)
						{
							expression = placeholder.WithPath(expression);
						}
					}

					break;
				}

				case ExpressionType.Invoke:
				{
					var pi = (InvocationExpression)expression;
					var ex = pi.Expression;

					if (ex.NodeType == ExpressionType.Quote)
						ex = ((UnaryExpression)ex).Operand;

					if (ex.NodeType == ExpressionType.Lambda)
					{
						var l   = (LambdaExpression)ex;
						var dic = new Dictionary<Expression,Expression>();

						for (var i = 0; i < l.Parameters.Count; i++)
							dic.Add(l.Parameters[i], pi.Arguments[i]);

						var pie = l.Body.Transform(dic, static (dic, wpi) => dic.TryGetValue(wpi, out var ppi) ? ppi : wpi);

						return CreatePlaceholder(context, ConvertToSql(context, pie), expression, alias : alias);
					}

					break;
				}

				case ExpressionType.TypeIs:
				{
					var condition = new SearchConditionWord();
					BuildSearchCondition(context, expression, flags, condition);
					return CreatePlaceholder(context, condition, expression, alias : alias);
				}

				case ExpressionType.TypeAs:
				{
					if (context == null)
						break;

					var unary     = (UnaryExpression)expression;
					var testExpr  = MakeIsPredicateExpression(context, Expression.TypeIs(unary.Operand, unary.Type));
					var trueCase  = Expression.Convert(unary.Operand, unary.Type);
					var falseCase = new DefaultValueExpression(DBLive, unary.Type);

					var cond = Expression.Condition(testExpr, trueCase, falseCase);

					return ConvertToSqlExpr(context, cond, flags : flags, unwrap : unwrap, columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, alias : alias);
				}

				case ChangeTypeExpression.ChangeTypeType:
					return CreatePlaceholder(context, ConvertToSql(context, ((ChangeTypeExpression)expression).Expression), expression, alias : alias);

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)expression;
					if (cnt.Value is IExpWord sql)
						return CreatePlaceholder(context, sql, expression, alias : alias);
					break;
				}

				case ExpressionType.New:
				case ExpressionType.MemberInit:
				{
					if (!flags.IsExpression() && ParseGenericConstructor(expression, flags, columnDescriptor, true) is SqlGenericConstructorExpression transformed)
					{
						return ConvertToSqlExpr(context, transformed, flags, unwrap, columnDescriptor, isPureExpression : isPureExpression, alias : alias);
					}

					var converted = ConvertSingleExpression(expression, flags.IsExpression());
					if (!ReferenceEquals(converted, expression))
					{
						return ConvertToSqlExpr(context, converted, flags, unwrap, columnDescriptor, isPureExpression : isPureExpression, alias : alias);
					}

					break;
				}

				case ExpressionType.Switch:
				{
					var switchExpression  = (SwitchExpression)expression;
					var d = switchExpression.DefaultBody == null ||
							switchExpression.DefaultBody is not UnaryExpression
							{
								NodeType : ExpressionType.Convert or ExpressionType.ConvertChecked, Operand : MethodCallExpression { Method : var m }
							} || m != ConvertBuilder.DefaultConverter;

					var ps = new IExpWord[switchExpression.Cases.Count * 2 + (d ? 1 : 0)];
					var svExpr = ConvertToSqlExpr(context, switchExpression.SwitchValue, flags, unwrap, columnDescriptor, isPureExpression, alias : alias);
					if (svExpr is not SqlPlaceholderExpression svPlaceholder)
						return SqlErrorExpression.EnsureError(svExpr, switchExpression.Type);
					var sv = svPlaceholder.Sql;

					for (var i = 0; i < switchExpression.Cases.Count; i++)
					{
						var sc = new SearchConditionWord(true);
						foreach (var testValue in switchExpression.Cases[i].TestValues)
						{
							var testValueExpr = ConvertToSqlExpr(context, testValue, flags, unwrap, columnDescriptor, isPureExpression, alias : alias);
							if (testValueExpr is not SqlPlaceholderExpression testPlaceholder)
								return SqlErrorExpression.EnsureError(testValueExpr, switchExpression.Type);

							sc.Add(new ExprExpr(sv, AffirmWord.Operator.Equal, testPlaceholder.Sql, CompareNullsAsValues ? true : null));
						}

						ps[i * 2]     = sc;
						ps[i * 2 + 1] = ConvertToSql(context, switchExpression.Cases[i].Body);
					}

					if (d)
						ps[ps.Length-1] = ConvertToSql(context, switchExpression.DefaultBody!);

					//TODO: Convert everything to SqlSimpleCaseExpression
					var cases = new List<CaseWord.CaseItem>(ps.Length);

					for (var i = 0; i < ps.Length; i += 2)
					{
						var caseExpr = ps[i];
						var value    = ps[i + 1];

						if (caseExpr is SearchConditionWord sc)
						{
							cases.Add(new CaseWord.CaseItem(sc, value));
						}
						else
							throw new InvalidOperationException();
					}

                        IExpWord? defaultExpression = null;

					if (d)
						defaultExpression = ps[ps.Length - 1];

					var caseExpression = new CaseWord(DBLive.dialect.mapping .GetDbDataType(switchExpression.Type), cases, defaultExpression);

					return CreatePlaceholder(context, caseExpression, expression, alias : alias);
				}

				/*default:
				{
					expression = BuildSqlExpression(new Dictionary<Expression, Expression>(), context, expression,
						flags, alias);

					break;
				}*/
			}

			if (expression is not SqlPlaceholderExpression && (expression.Type == typeof(bool) || expression.Type == typeof(bool?)) && _convertedPredicates.Add(expression))
			{
				var predicate = ConvertPredicate(context, expression, flags, out var error);
				if (predicate == null)
					return error!.WithType(expression.Type);

				_convertedPredicates.Remove(expression);
				return CreatePlaceholder(context, new SearchConditionWord(false, predicate), expression, alias : alias);
			}

			return expression;
		}

		static ObjectPool<TranslationContext> _translationContexts = new ObjectPool<TranslationContext>(() => new TranslationContext(), c => c.Cleanup(), 100);

		public Expression? TranslateMember(IBuildContext? context, ProjectFlags flags, EntityColumn? columnDescriptor, string? alias, Expression memberExpression)
		{
			if (context == null)
				return null;

			if (memberExpression is MethodCallExpression || memberExpression is MemberExpression || memberExpression is NewExpression)
			{
				if (IsAlreadyTranslated(context, flags, columnDescriptor, memberExpression, out var cacheKey, out var translateMember))
				{
					return translateMember;
				}

				using var translationContext = _translationContexts.Allocate();

				translationContext.Value.Init(this, context, columnDescriptor, alias);

				var translated = _memberTranslator.Translate(translationContext.Value, memberExpression, GetTranslationFlags(flags));

				if (translated != null)
				{
					if (!flags.IsTest())
						translated = UpdateNesting(context, translated);

					if (translated is SqlPlaceholderExpression placeholder)
					{
						_cachedSql.Remove(cacheKey);
						_cachedSql.Add(cacheKey, placeholder);
					}
				}

				return translated;
			}

			return null;
		}

		bool IsAlreadyTranslated(IBuildContext? context, ProjectFlags flags, EntityColumn? columnDescriptor, Expression memberExpression, out SqlCacheKey cacheKey, [NotNullWhen(true)] out Expression? translatedExpression)
		{
			var cacheFlags = flags & ~ProjectFlags.Keys;
			cacheFlags &= ~ProjectFlags.ForExtension;

			cacheKey = new SqlCacheKey(memberExpression, null, columnDescriptor, context?.SelectQuery, cacheFlags);

			if (_cachedSql.TryGetValue(cacheKey, out var sqlExpr))
			{
				if (sqlExpr is SqlPlaceholderExpression cachedPlaceholder)
				{
					translatedExpression = cachedPlaceholder.WithTrackingPath(memberExpression);
					return true;
				}

				{
					translatedExpression = sqlExpr;
					return true;
				}
			}

			if (cacheFlags.IsExpression())
			{
				cacheFlags = cacheFlags.SqlFlag();
				cacheKey   = new SqlCacheKey(memberExpression, null, columnDescriptor, context?.SelectQuery, cacheFlags);
				if (_cachedSql.TryGetValue(cacheKey, out var asSql))
				{
					translatedExpression = asSql;
					return true;
				}
			}

			translatedExpression = null;
			return false;
		}

		public IExpWord? TryConvertFormatToSql(IBuildContext? context, MethodCallExpression mc, bool isPureExpression, ProjectFlags flags)
		{
			// TODO: move PrepareRawSqlArguments to more correct location
			TableBuilder.PrepareRawSqlArguments(mc, null,
				out var format, out var arguments);

			var sqlArguments = new List<IExpWord>();
			foreach (var a in arguments)
			{
				if (!TryConvertToSql(context, a, flags, null, out var sqlExpr, out _))
					return null;

				sqlArguments.Add(sqlExpr);
			}

			if (isPureExpression)
				return new ExpressionWord(mc.Type, format, PrecedenceLv.Primary, sqlArguments.ToArray());

			return QueryHelper.ConvertFormatToConcatenation(format, sqlArguments);
		}

		public Expression ConvertExtensionToSql(IBuildContext context, ProjectFlags flags, Sql.ExpressionAttribute attr, MethodCallExpression mc, bool checkAggregateRoot)
		{




			var currentContext = context;

			if ((attr.IsAggregate || attr.IsWindowFunction) && checkAggregateRoot)
			{
				var sequenceRef = new ContextRefExpression(context.ElementType, context);

				var rootContext = GetRootContext(context, sequenceRef, true);

				currentContext = rootContext?.BuildContext ?? currentContext;

				if (currentContext is GroupByBuilder.GroupByContext groupCtx)
				{
					currentContext = groupCtx.SubQuery;
				}
			}

			// Second attempt probably conversion failed and switched to client side evaluation
			if (mc.Find(1, (_, e) => e is PlaceholderExpression { PlaceholderType: PlaceholderType.Closure }) != null)
				return mc;

			var sqlExpression = attr.GetExpression(
				(this_: this, context: currentContext, flags),
				DBLive,
				this,
				currentContext.SelectQuery,
				mc,
				static (context, e, descriptor, inline) => context.this_.ConvertToExtensionSql(context.context, context.flags, e, descriptor, inline));

			//DataContext.InlineParameters = inlineParameters;

			if (sqlExpression is SqlPlaceholderExpression placeholder)
			{
				RegisterExtensionAccessors(mc);

				placeholder = placeholder.WithSql(PosProcessCustomExpression(mc, placeholder.Sql, NullabilityContext.GetContext(placeholder.SelectQuery)));

				sqlExpression = placeholder.WithPath(mc);
			}

			return sqlExpression;
		}

		public IExpWord PosProcessCustomExpression(Expression expression, IExpWord sqlExpression, NullabilityContext nullabilityContext)
		{
			if (sqlExpression is ExpressionWord { Expr: "{0}", Parameters.Length: 1 } expr)
			{
				var expressionNull = nullabilityContext.CanBeNull(sqlExpression);
				var argNull        = nullabilityContext.CanBeNull(expr.Parameters[0]);

				if (expressionNull != argNull)
					return NullabilityWord.ApplyNullability(expr.Parameters[0], expressionNull);

				return expr.Parameters[0];
			}

			return sqlExpression;
		}

        IExpWord? ConvertToInlinedSqlExpression(IBuildContext? context, Expression newExpr)
		{
            IExpWord? innerSql;
			innerSql = EvaluateExpression<IExpWord>(newExpr);

			if (innerSql == null)
				return null;

			var param = ParametersContext.BuildParameter(context, newExpr, null, doNotCheckCompatibility : true);
			if (param == null)
			{
				return null;
			}

			return new InlinedSqlWord(param.SqlParameter, innerSql);
		}

		public IExpWord? ConvertToSqlConvertible(IBuildContext? context, Expression expression)
		{
			if (EvaluateExpression(Expression.Convert(expression, typeof(IToSqlConverter))) is not IToSqlConverter converter)
				throw new LinqToDBException($"Expression '{expression}' cannot be converted to `IToSqlConverter`");

			var innerExpr = converter.ToSql(converter);

			var param = ParametersContext.BuildParameter(context, expression, null, doNotCheckCompatibility : true);
			if (param == null)
			{
				return null;
			}

			return new InlinedToSqlWord(param.SqlParameter, innerExpr);
		}

		readonly HashSet<Expression> _convertedPredicates = new ();

		#endregion

		#region IsServerSideOnly

		public bool IsServerSideOnly(Expression expr, bool inProjection)
		{
			return _optimizationContext.IsServerSideOnly(expr, inProjection);
		}

		#endregion

		#region CanBeConstant

		internal bool CanBeConstant(Expression expr)
		{
			if (!ParametersContext.CanBeConstant(expr))
			{
				return false;
			}
			return _optimizationContext.CanBeConstant(expr);
		}

		#endregion

		#region CanBeCompiled

		public bool CanBeCompiled(Expression expr, bool inProjection)
		{
			return _optimizationContext.CanBeCompiled(expr, inProjection);
		}

		#endregion

		#region Build Constant

		readonly Dictionary<(Expression, EntityColumn?, int), ValueWord> _constants = new ();


        #endregion

        #region Predicate Converter

        IAffirmWord? ConvertPredicate(IBuildContext? context, Expression expression, ProjectFlags flags, out SqlErrorExpression? error)
		{
			error = null;

            IAffirmWord? CheckExpression(Expression expr, ref SqlErrorExpression? resultError)
			{
				if (expr is SqlPlaceholderExpression { Sql: SearchConditionWord sc })
					return sc;

				resultError = SqlErrorExpression.EnsureError(context, expr);

				return null;
			}

            IExpWord IsCaseSensitive(MethodCallExpression mc)
			{
				if (mc.Arguments.Count <= 1)
					return new ValueWord(typeof(bool?), null);

				if (!typeof(StringComparison).IsSameOrParentOf(mc.Arguments[1].Type))
					return new ValueWord(typeof(bool?), null);

				var arg = mc.Arguments[1];

				if (arg.NodeType == ExpressionType.Constant || arg.NodeType == ExpressionType.Default)
				{
					var comparison = (StringComparison)(EvaluateExpression(arg) ?? throw new InvalidOperationException());
					return new ValueWord(comparison is StringComparison.CurrentCulture
										           or StringComparison.InvariantCulture
										           or StringComparison.Ordinal);
				}

				var variable   = Expression.Variable(typeof(StringComparison), "c");
				var assignment = Expression.Assign(variable, arg);
				var expr       = (Expression)Expression.Equal(variable, Expression.Constant(StringComparison.CurrentCulture));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.InvariantCulture)));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.Ordinal)));
				expr = Expression.Block(new[] { variable }, assignment, expr);

				var parameter = ParametersContext.BuildParameter(context, expr, columnDescriptor : null, forceConstant : true)!;
				parameter.SqlParameter.IsQueryParameter = false;

				return parameter.SqlParameter;
			}

			if (CanBeCompiled(expression, false))
			{
				var param = _parametersContext.BuildParameter(context, expression, null, buildParameterType: ParametersContext.BuildParameterType.Bool);
				if (param != null)
				{
					return new Expr(param.SqlParameter);
				}
			}

			switch (expression.NodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					var e = (BinaryExpression)expression;

					var left  = RemoveNullPropagation(context!, e.Left, flags, false);
					var right = RemoveNullPropagation(context!, e.Right, flags, false);

					var newExpr = e.Update(left, e.Conversion, right);

					left  = newExpr.Left;
					right = newExpr.Right;

					return CheckExpression(ConvertCompareExpression(context, newExpr.NodeType, left, right, flags, newExpr), ref error);
				}

				case ExpressionType.Call:
				{
					var e = (MethodCallExpression)expression;

                        IAffirmWord? predicate = null;

					if (e.Method.Name          == nameof(Sql.Alias) && e.Object == null && e.Arguments.Count == 2 &&
						e.Method.DeclaringType == typeof(Sql))
					{
						predicate = ConvertPredicate(context, e.Arguments[0], flags, out error);
						return predicate;
					}

					if (e.Method.Name == "Equals" && e.Object != null && e.Arguments.Count == 1)
						return CheckExpression(ConvertCompareExpression(context, ExpressionType.Equal, e.Object, e.Arguments[0], flags), ref error);

					if (e.Method.DeclaringType == typeof(string))
					{
						switch (e.Method.Name)
						{
							case "Contains"   : predicate = CreateStringPredicate(context, e, mooSQL.data.model.affirms.SearchString.SearchKind.Contains,   IsCaseSensitive(e), flags); break;
							case "StartsWith" : predicate = CreateStringPredicate(context, e, mooSQL.data.model.affirms.SearchString.SearchKind.StartsWith, IsCaseSensitive(e), flags); break;
							case "EndsWith"   : predicate = CreateStringPredicate(context, e, mooSQL.data.model.affirms.SearchString.SearchKind.EndsWith,   IsCaseSensitive(e), flags); break;
						}
					}
					else if (e.Method.Name == "Contains")
					{
						if (e.Method.DeclaringType  == typeof(Enumerable) ||
						    (e.Method.DeclaringType == typeof(Queryable) && e.Arguments.Count == 2 && CanBeCompiled(e.Arguments[0], false)) ||
							typeof(IList).IsSameOrParentOf(e.Method.DeclaringType!) ||
							typeof(ICollection<>).IsSameOrParentOf(e.Method.DeclaringType!) ||
							typeof(IReadOnlyCollection<>).IsSameOrParentOf(e.Method.DeclaringType!))
						{
							predicate = ConvertInPredicate(context!, e);
						}
					}
					else if (e.Method.Name == "ContainsValue" && typeof(Dictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!))
					{
						var args = e.Method.DeclaringType!.GetGenericArguments(typeof(Dictionary<,>))!;
						var minf = EnumerableMethods
								.First(static m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[1]);

						var expr = Expression.Call(
								minf,
								ExpressionHelper.PropertyOrField(e.Object!, "Values"),
								e.Arguments[0]);

						predicate = ConvertInPredicate(context!, expr);
					}
					else if (e.Method.Name == "ContainsKey" &&
						(typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!) ||
						 typeof(IReadOnlyDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!)))
					{
						var type = typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType!) ? typeof(IDictionary<,>) : typeof(IReadOnlyDictionary<,>);
						var args = e.Method.DeclaringType!.GetGenericArguments(type)!;
						var minf = EnumerableMethods
								.First(static m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[0]);

						var expr = Expression.Call(
								minf,
								ExpressionHelper.PropertyOrField(e.Object!, "Keys"),
								e.Arguments[0]);

						predicate = ConvertInPredicate(context!, expr);
					}

#if NETFRAMEWORK
					else if (e.Method == ReflectionHelper.Functions.String.Like11) predicate = ConvertLikePredicate(context!, e, flags);
					else if (e.Method == ReflectionHelper.Functions.String.Like12) predicate = ConvertLikePredicate(context!, e, flags);
#endif
					else if (e.Method == ReflectionHelper.Functions.String.Like21) predicate = ConvertLikePredicate(context!, e, flags);
					else if (e.Method == ReflectionHelper.Functions.String.Like22) predicate = ConvertLikePredicate(context!, e, flags);

					if (predicate != null)
						return predicate;

					var attr = e.Method.GetExpressionAttribute(DBLive);

					if (attr != null && attr.GetIsPredicate(expression))
						break;

					var processed = MakeExpression(context, expression, flags);
					if (!ReferenceEquals(processed, expression))
					{
						return ConvertPredicate(context, processed, flags, out error);
					}

					break;
				}

				case ExpressionType.Conditional:
					return new ExprExpr(
                            ConvertToSql(context, expression),
                            AffirmWord.Operator.Equal,
							new ValueWord(true), null);

				case ExpressionType.TypeIs:
				{
					var e   = (TypeBinaryExpression)expression;
					var contextRef = GetRootContext(context, e.Expression, false);

					if (contextRef != null && SequenceHelper.GetTableContext(contextRef.BuildContext) != null)
						return MakeIsPredicate(contextRef.BuildContext, e, flags, out error);

					break;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var e = (UnaryExpression)expression;

					if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
						return ConvertPredicate(context, e.Operand, flags, out error);

					break;
				}
			}

			if (!TryConvertToSql(context, expression, flags, null, out var ex, out error))
				return null;

			if (ExpressionWord.NeedsEqual(ex))
			{
				var descriptor = QueryHelper.GetColumnDescriptor(ex);

				if (ex is ColumnWord col)
					ex = NullabilityWord.ApplyNullability(ex, NullabilityContext.GetContext(col.Parent));

				var trueValue  = ConvertToSql(context, ExpressionInstances.True, columnDescriptor: descriptor);
				var falseValue = ConvertToSql(context, ExpressionInstances.False, columnDescriptor: descriptor);

				return new IsTrue(ex, trueValue, falseValue, DBLive.dialect.Option.CompareNullsAsValues ? false : null, false);
			}

			if (ex is IAffirmWord expPredicate)
				return expPredicate;

			return new mooSQL.data.model.affirms.Expr(ex);
		}

		#region ConvertCompare

		static LambdaExpression BuildMemberPathLambda(Expression path)
		{
			var memberPath = new List<MemberInfo>();

			var current = path;
			do
			{
				if (current is MemberExpression me)
				{
					current = me.Expression!;
					memberPath.Add(me.Member);
				}
				else
					break;

			} while (true);

			var        param = Expression.Parameter(current.Type, "o");
			Expression body  = param;
			for (int i = memberPath.Count - 1; i >= 0; i--)
			{
				body = Expression.MakeMemberAccess(body, memberPath[i]);
			}

			return Expression.Lambda(body, param);
		}

		public SearchConditionWord? TryGenerateComparison(
			IBuildContext? context,
			Expression     left,
			Expression     right,
			ProjectFlags   flags = ProjectFlags.SQL)
		{
			var expr = ConvertCompareExpression(context, ExpressionType.Equal, left, right, flags);
			if (expr is SqlPlaceholderExpression { Sql: SearchConditionWord sc })
				return sc;

			return null;
		}

		public SearchConditionWord GenerateComparison(
			IBuildContext? context,
			Expression     left,
			Expression     right,
			ProjectFlags   flags = ProjectFlags.SQL)
		{
			var expr = ConvertCompareExpression(context, ExpressionType.Equal, left, right, flags);
			if (expr is SqlPlaceholderExpression { Sql: SearchConditionWord sc })
				return sc;
			if (expr is SqlErrorExpression error)
				throw error.CreateException();

			throw new SqlErrorExpression($"Could not compare '{left}' with {right}", typeof(bool)).CreateException();
		}

		Expression ConvertCompareExpression(IBuildContext? context, ExpressionType nodeType, Expression left, Expression right, ProjectFlags flags, Expression? originalExpression = null)
		{
			Expression GetOriginalExpression()
			{
				if (originalExpression != null)
					return originalExpression;

				var rightExpr = right;
				var leftExpr  = left;
				if (rightExpr.Type != leftExpr.Type)
				{
					if (rightExpr.Type.CanConvertTo(leftExpr.Type))
						rightExpr = Expression.Convert(rightExpr, leftExpr.Type);
					else if (left.Type.CanConvertTo(leftExpr.Type))
						leftExpr = Expression.Convert(leftExpr, right.Type);
				}
				else
				{
					if (nodeType == ExpressionType.Equal || nodeType == ExpressionType.NotEqual)
					{
						// Fore generating Path for SqlPlaceholderExpression
						if (!rightExpr.Type.IsPrimitive)
						{
							return new SqlPathExpression(
								new[] { leftExpr, Expression.Constant(nodeType), rightExpr },
								typeof(bool));
						}
					}
				}

				return Expression.MakeBinary(nodeType, leftExpr, rightExpr);
			}

			Expression GenerateNullComparison(Expression placeholdersExpression, bool isNot)
			{
				List<Expression> expressions = new();
				if (!CollectNullCompareExpressions(context, placeholdersExpression, expressions) || expressions.Count == 0)
					return GetOriginalExpression();

				List<SqlPlaceholderExpression> placeholders = new(expressions.Count);
				List<SqlPlaceholderExpression>? notNull      = null;

				var nullability = NullabilityContext.NonQuery;

				foreach (var expression in expressions)
				{
					var predicateExpr = ConvertToSqlExpr(context, expression, flags.SqlFlag());
					if (predicateExpr is SqlPlaceholderExpression placeholder)
					{
						if (!placeholder.Sql.CanBeNullable(nullability))
						{
							placeholders.Clear();
							placeholders.Add(placeholder);
							notNull = placeholders;
							break;
						}
						else
						{
							placeholders.Add(placeholder);
						}
					}
				}

				if (placeholders.Count == 0)
					return GetOriginalExpression();

				if (notNull == null)
					notNull = placeholders;

				var searchCondition = new SearchConditionWord(isNot);
				foreach (var placeholder in notNull)
				{
					var sql = placeholder.Sql;
					searchCondition.Predicates.Add(new IsNull(sql, isNot));
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}

			Expression GeneratePathComparison(Expression leftOriginal, Expression leftParsed, Expression rightOriginal, Expression rightParsed)
			{
				var predicateExpr = GeneratePredicate(leftOriginal, leftParsed, rightOriginal, rightParsed);
				if (predicateExpr == null)
					return GetOriginalExpression();

				var converted = ConvertToSqlExpr(context, predicateExpr, flags);
				if (converted is not SqlPlaceholderExpression)
					converted = GetOriginalExpression();

				return converted;
			}

			Expression? GeneratePredicate(Expression leftOriginal, Expression leftParsed, Expression rightOriginal, Expression rightParsed)
			{
				Expression? predicateExpr = null;

				if (leftParsed is SqlGenericConstructorExpression genericLeft)
				{
					predicateExpr = BuildPredicateExpression(genericLeft, null, rightOriginal);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (rightParsed is SqlGenericConstructorExpression genericRight)
				{
					predicateExpr = BuildPredicateExpression(genericRight, null, leftOriginal);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (leftParsed is ConditionalExpression condLeft)
				{
					if (condLeft.IfTrue is SqlGenericConstructorExpression genericTrue)
					{
						predicateExpr = BuildPredicateExpression(genericTrue, leftOriginal, rightOriginal);
					}
					else if (condLeft.IfFalse is SqlGenericConstructorExpression genericFalse)
					{
						predicateExpr = BuildPredicateExpression(genericFalse, leftOriginal, rightOriginal);
					}

					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, condLeft.IfTrue, rightOriginal, rightParsed);
					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, condLeft.IfFalse, rightOriginal, rightParsed);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (rightParsed is ConditionalExpression condRight)
				{
					if (condRight.IfTrue is SqlGenericConstructorExpression genericTrue)
					{
						predicateExpr = BuildPredicateExpression(genericTrue, leftOriginal, rightOriginal);
					}
					else if (condRight.IfFalse is SqlGenericConstructorExpression genericFalse)
					{
						predicateExpr = BuildPredicateExpression(genericFalse, leftOriginal, rightOriginal);
					}

					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, leftParsed, condRight.IfTrue, rightParsed);
					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, leftParsed, condRight.IfFalse, rightParsed);
				}

				if (predicateExpr != null)
					return predicateExpr;

				return predicateExpr;
			}

			Expression? BuildPredicateExpression(SqlGenericConstructorExpression genericConstructor, Expression? rootLeft, Expression rootRight)
			{
				if (genericConstructor.Assignments.Count == 0)
					return null;

				var operations = genericConstructor.Assignments
					.Select(a => Expression.Equal(
						rootLeft == null ? a.Expression : Expression.MakeMemberAccess(rootLeft, a.MemberInfo),
						Expression.MakeMemberAccess(rootRight, a.MemberInfo))
					);

				var result = (Expression)operations.Aggregate(Expression.AndAlso);
				if (nodeType == ExpressionType.NotEqual)
					result = Expression.Not(result);

				return result;
			}

			Expression GenerateConstructorComparison(SqlGenericConstructorExpression leftConstructor, SqlGenericConstructorExpression rightConstructor)
			{
				var strict = leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.Full ||
							 rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.Full ||
							 (leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.New &&
							  rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.New) ||
							 (leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.MemberInit &&
							  rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.MemberInit);

				var isNot           = nodeType == ExpressionType.NotEqual;
				var searchCondition = new SearchConditionWord(isNot);
				var usedMembers     = new HashSet<MemberInfo>(MemberInfoEqualityComparer.Default);

				foreach (var leftAssignment in leftConstructor.Assignments)
				{
					var found = rightConstructor.Assignments.FirstOrDefault(a =>
						MemberInfoEqualityComparer.Default.Equals(a.MemberInfo, leftAssignment.MemberInfo));

					if (found == null && strict)
					{
						// fail fast and prepare correct error expression
						return CreateSqlError(context, Expression.MakeMemberAccess(right, leftAssignment.MemberInfo));
					}

					var rightExpression = found?.Expression;
					if (rightExpression == null)
					{
						rightExpression = Expression.Default(leftAssignment.Expression.Type);
					}
					else
					{
						usedMembers.Add(found!.MemberInfo);
					}

					var predicateExpr = ConvertCompareExpression(context, nodeType, leftAssignment.Expression, rightExpression, flags);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SearchConditionWord sc })
					{
						if (strict)
							return GetOriginalExpression();
						continue;
					}

					searchCondition.Predicates.Add(sc.MakeNot(isNot));
				}

				foreach (var rightAssignment in rightConstructor.Assignments)
				{
					if (usedMembers.Contains(rightAssignment.MemberInfo))
						continue;

					if (strict)
					{
						// fail fast and prepare correct error expression
						return CreateSqlError(context, Expression.MakeMemberAccess(left, rightAssignment.MemberInfo));
					}

					var leftExpression = Expression.Default(rightAssignment.Expression.Type);

					var predicateExpr = ConvertCompareExpression(context, nodeType, leftExpression, rightAssignment.Expression, flags);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SearchConditionWord sc })
					{
						if (strict)
							return predicateExpr;
						continue;
					}

					searchCondition.Predicates.Add(sc.MakeNot(isNot));
				}

				if (usedMembers.Count == 0)
				{
					if (leftConstructor.Parameters.Count > 0 && leftConstructor.Parameters.Count == rightConstructor.Parameters.Count)
					{
						for (var index = 0; index < leftConstructor.Parameters.Count; index++)
						{
							var leftParam  = leftConstructor.Parameters[index];
							var rightParam = rightConstructor.Parameters[index];

							var predicateExpr = ConvertCompareExpression(context, nodeType, leftParam.Expression, rightParam.Expression, flags);
							if (predicateExpr is not SqlPlaceholderExpression { Sql: SearchConditionWord sc })
							{
								if (strict)
									return GetOriginalExpression();
								continue;
							}

							searchCondition.Predicates.Add(sc.MakeNot(isNot));
						}

					}
					else
						return GetOriginalExpression();
				}

				return CreatePlaceholder(context, searchCondition, GetOriginalExpression());
			}

			if (!RestoreCompare(ref left, ref right))
				RestoreCompare(ref right, ref left);

			if (context == null)
				throw new InvalidOperationException();

            IExpWord? l = null;
            IExpWord? r = null;

			var nullability = NullabilityContext.GetContext(context.SelectQuery);

			var keysFlag         = (flags & ~ProjectFlags.ForExtension) | ProjectFlags.Keys;
			var columnDescriptor = SuggestColumnDescriptor(context, left, right, keysFlag);
			var leftExpr         = ConvertToSqlExpr(context, left,  keysFlag, columnDescriptor : columnDescriptor);
			var rightExpr        = ConvertToSqlExpr(context, right, keysFlag, columnDescriptor : columnDescriptor);

			var compareNullsAsValues = CompareNullsAsValues;

			//SQLRow case when needs to add Single
			//
			if (leftExpr is SqlPlaceholderExpression { Sql: RowWord } && rightExpr is not SqlPlaceholderExpression)
			{
				var elementType = TypeHelper.GetEnumerableElementType(rightExpr.Type);
				var singleCall  = Expression.Call(Methods.Enumerable.Single.MakeGenericMethod(elementType), right);
				rightExpr = ConvertToSqlExpr(context, singleCall, keysFlag, columnDescriptor : columnDescriptor);
			}
			else if (rightExpr is SqlPlaceholderExpression { Sql: RowWord } &&
			         leftExpr is not SqlPlaceholderExpression)
			{
				var elementType = TypeHelper.GetEnumerableElementType(leftExpr.Type);
				var singleCall  = Expression.Call(Methods.Enumerable.Single.MakeGenericMethod(elementType), left);
				leftExpr = ConvertToSqlExpr(context, singleCall, keysFlag, columnDescriptor : columnDescriptor);
			}

			leftExpr  = RemoveNullPropagation(leftExpr, true);
			rightExpr = RemoveNullPropagation(rightExpr, true);

			if (leftExpr is SqlErrorExpression leftError)
				return leftError.WithType(typeof(bool));

			if (rightExpr is SqlErrorExpression rightError)
				return rightError.WithType(typeof(bool));

			if (leftExpr is SqlPlaceholderExpression placeholderLeft)
			{
				l = placeholderLeft.Sql;
			}

			if (rightExpr is SqlPlaceholderExpression placeholderRight)
			{
				r = placeholderRight.Sql;
			}

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					var isNot = nodeType == ExpressionType.NotEqual;

					if (l != null && r != null)
						break;

					leftExpr  = ParseGenericConstructor(leftExpr, flags, columnDescriptor, true);
					rightExpr = ParseGenericConstructor(rightExpr, flags, columnDescriptor, true);

					if (SequenceHelper.UnwrapDefaultIfEmpty(leftExpr) is SqlGenericConstructorExpression leftGenericConstructor &&
					    SequenceHelper.UnwrapDefaultIfEmpty(rightExpr) is SqlGenericConstructorExpression rightGenericConstructor)
					{
						return GenerateConstructorComparison(leftGenericConstructor, rightGenericConstructor);
					}

					if (l is ValueWord lv && lv.Value == null || left.IsNullValue())
					{
						rightExpr = BuildSqlExpression(context, rightExpr, flags);

						if (rightExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SearchConditionWord rightSearchCond } } && rightSearchCond.Predicates.Count == 1)
						{
							var rightPredicate  = rightSearchCond.Predicates[0];
							var localIsNot = isNot;

							if (rightPredicate is IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, isnull), GetOriginalExpression());

								return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, new IsNull(isnull.Expr1, !isnull.IsNot)), GetOriginalExpression());
							}
						}

						return GenerateNullComparison(rightExpr, isNot);
					}

					if (r is ValueWord rv && rv.Value == null || right.IsNullValue())
					{
						leftExpr = BuildSqlExpression(context, leftExpr, flags);

						if (leftExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SearchConditionWord leftSearchCond } } && leftSearchCond.Predicates.Count == 1)
						{
							var leftPredicate  = leftSearchCond.Predicates[0];
							var localIsNot = isNot;

							if (leftPredicate is IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, isnull), GetOriginalExpression());

								return CreatePlaceholder(context, (IExpWord)new SearchConditionWord(false, new IsNull(isnull.Expr1, !isnull.IsNot)), GetOriginalExpression());
							}
						}

						return GenerateNullComparison(leftExpr, isNot);
					}

					if (l == null || r == null)
					{
						var pathComparison = GeneratePathComparison(left, SequenceHelper.UnwrapDefaultIfEmpty(leftExpr), right, SequenceHelper.UnwrapDefaultIfEmpty(rightExpr));

						return pathComparison;
					}

					break;
			}

			var op = nodeType switch
			{
                ExpressionType.Equal              => AffirmWord.Operator.Equal,
                ExpressionType.NotEqual           => AffirmWord.Operator.NotEqual,
                ExpressionType.GreaterThan        => AffirmWord.Operator.Greater,
                ExpressionType.GreaterThanOrEqual => AffirmWord.Operator.GreaterOrEqual,
                ExpressionType.LessThan           => AffirmWord.Operator.Less,
                ExpressionType.LessThanOrEqual    => AffirmWord.Operator.LessOrEqual,
				_                                 => throw new InvalidOperationException(),
			};

			if ((left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked || right.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) && (op == AffirmWord.Operator.Equal || op == AffirmWord.Operator.NotEqual))
			{
				var p = ConvertEnumConversion(context!, left, op, right);
				if (p != null)
					return CreatePlaceholder(context, new SearchConditionWord(false, p), GetOriginalExpression());
			}

			if (l is null)
			{
				if (!TryConvertToSql(context, left, flags, columnDescriptor : columnDescriptor, out var lConverted, out _))
					return GetOriginalExpression();
				l = lConverted;
			}

			if (r is null)
			{
				if (!TryConvertToSql(context, right, flags, columnDescriptor : columnDescriptor, out var rConverted, out _))
					return GetOriginalExpression();
				r = rConverted;
			}

			var lOriginal = l;
			var rOriginal = r;

			l = QueryHelper.UnwrapExpression(l, checkNullability: true);
			r = QueryHelper.UnwrapExpression(r, checkNullability: true);

			if (l is ValueWord lValue)
				lValue.ValueType = GetDataType(r, lValue.ValueType, DBLive);

			if (r is ValueWord rValue)
				rValue.ValueType = GetDataType(l, rValue.ValueType, DBLive);

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					if (!context!.SelectQuery.IsParameterDependent &&
						(l is ParameterWord && lOriginal.CanBeNullable(nullability) || r is ParameterWord && r.CanBeNullable(nullability)))
					{
						context.SelectQuery.IsParameterDependent = true;
					}

					break;
			}

            IAffirmWord? predicate = null;

			var isEquality = op == AffirmWord.Operator.Equal || op == AffirmWord.Operator.NotEqual
				? op == AffirmWord.Operator.Equal
				: (bool?)null;

			// TODO: maybe remove
			if (l is SearchConditionWord lsc)
			{
				if (isEquality != null & IsBooleanConstant(rightExpr, out var boolRight) && boolRight != null)
				{
					predicate = lsc.MakeNot(boolRight != isEquality);
				}
			}

			// TODO: maybe remove
			if (r is SearchConditionWord rsc)
			{
				if (isEquality != null & IsBooleanConstant(rightExpr, out var boolLeft) && boolLeft != null)
				{
					predicate = rsc.MakeNot(boolLeft != isEquality);
				}
			}

			if (predicate == null)
			{
				if (isEquality != null)
				{
					bool?           value;
                    IExpWord? expression  = null;

					if (IsBooleanConstant(left, out value))
					{
						if (l.NodeType != ClauseType.SqlParameter)
						{
							expression = rOriginal;
						}
					}
					else if (IsBooleanConstant(right, out value))
					{
						if (r.NodeType != ClauseType.SqlParameter)
						{
							expression = lOriginal;
						}
					}

					if (value != null
						&& expression != null
						&& !(expression.NodeType == ClauseType.SqlValue && ((ValueWord)expression).Value == null))
					{
						var isNot = !value.Value;
						var withNull = false;
						if (op == AffirmWord.Operator.NotEqual)
						{
							isNot = !isNot;
							withNull = true;
						}
						var descriptor = QueryHelper.GetColumnDescriptor(expression);
						var trueValue  = ConvertToSql(context, ExpressionInstances.True,  unwrap: false, columnDescriptor: descriptor);
						var falseValue = ConvertToSql(context, ExpressionInstances.False, unwrap: false, columnDescriptor: descriptor);

						if (trueValue.NodeType  == ClauseType.SqlValue &&
						    falseValue.NodeType == ClauseType.SqlValue)
						{
							var withNullValue = compareNullsAsValues
								? withNull
								: (bool?)null;
							predicate = new IsTrue(expression, trueValue, falseValue, withNullValue, isNot);
						}
					}
				}

				// Force nullability
				if (QueryHelper.IsNullValue(lOriginal))
				{
					rOriginal = NullabilityWord.ApplyNullability(rOriginal, true);
					predicate = new IsNull(rOriginal, op == AffirmWord.Operator.NotEqual);
				}
				else if (QueryHelper.IsNullValue(rOriginal))
				{
					lOriginal = NullabilityWord.ApplyNullability(lOriginal, true);
					predicate = new IsNull(lOriginal, op == AffirmWord.Operator.NotEqual);
				}

				if (predicate == null)
				{
					if (compareNullsAsValues)
					{
						if (lOriginal is ColumnWord colLeft)
							lOriginal = NullabilityWord.ApplyNullability(lOriginal, NullabilityContext.GetContext(colLeft.Parent));
						else if (lOriginal is FieldWord)
							lOriginal = NullabilityWord.ApplyNullability(lOriginal, NullabilityContext.NonQuery);

						if (rOriginal is ColumnWord colRight)
							rOriginal = NullabilityWord.ApplyNullability(rOriginal, NullabilityContext.GetContext(colRight.Parent));
						else if (rOriginal is FieldWord)
							rOriginal = NullabilityWord.ApplyNullability(rOriginal, NullabilityContext.NonQuery);

						lOriginal = NullabilityWord.ApplyNullability(lOriginal, nullability);
						rOriginal = NullabilityWord.ApplyNullability(rOriginal, nullability);
					}

					predicate = new ExprExpr(lOriginal, op, rOriginal,
						compareNullsAsValues
							? true
							: null);
				}
			}

			return CreatePlaceholder(context, new SearchConditionWord(false, predicate), GetOriginalExpression());
		}

		public static List<SqlPlaceholderExpression> CollectPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					list.Add(placeholder);
				}
			});

			return result;
		}

		public static IEnumerable<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> CollectPlaceholders2(
			Expression expression, List<MemberInfo> currentPath)
		{
			IEnumerable<(SqlPlaceholderExpression placeholder, MemberInfo[] path)> Collect(Expression expr, Stack<MemberInfo> current)
			{
				if (expr is SqlPlaceholderExpression placeholder)
					yield return (placeholder, current.ToArray());

				if (expr is SqlGenericConstructorExpression generic)
				{
					foreach (var assignment in generic.Assignments)
					{
						current.Push(assignment.MemberInfo);
						foreach (var found in Collect(assignment.Expression, current))
							yield return found;
						current.Pop();
					}

					foreach (var parameter in generic.Parameters)
					{
						if (parameter.MemberInfo == null)
							throw new LinqException("Parameters which are not mapped to field are not supported.");

						current.Push(parameter.MemberInfo);
						foreach (var found in Collect(parameter.Expression, current))
							yield return found;
						current.Pop();
					}
				}
			}

			foreach (var found in Collect(expression, new (currentPath)))
				yield return found;
		}

		public static List<SqlPlaceholderExpression> CollectDistinctPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (!list.Contains(placeholder))
						list.Add(placeholder);
				}
			});

			return result;
		}

		public bool CollectNullCompareExpressions(IBuildContext context, Expression expression, List<Expression> result)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
				case ExpressionType.Default:
				{
					result.Add(expression);
					return true;
				}
			}

			if (expression is SqlPlaceholderExpression or DefaultValueExpression)
			{
				result.Add(expression);
				return true;
			}

			if (expression is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					if (!CollectNullCompareExpressions(context, assignment.Expression, result))
						return false;
				}

				foreach (var parameter in generic.Parameters)
				{
					if (!CollectNullCompareExpressions(context, parameter.Expression, result))
						return false;
				}

				return true;
			}

			if (expression is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				result.AddRange(defaultIfEmptyExpression.NotNullExpressions);
				return true;
			}

			if (expression is SqlEagerLoadExpression)
				return true;

			return false;
		}

		private static bool IsBooleanConstant(Expression expr, out bool? value)
		{
			value = null;
			if (expr.Type == typeof(bool) || expr.Type == typeof(bool?))
			{
				expr = expr.Unwrap();
				if (expr is ConstantExpression c)
				{
					value = c.Value as bool?;
					return true;
				}
				else if (expr is DefaultExpression)
				{
					value = expr.Type == typeof(bool) ? false : null;
					return true;
				}
				else if (expr is SqlPlaceholderExpression palacehoder)
				{
					if (palacehoder.Sql is ValueWord sqlValue)
					{
						value = sqlValue.Value as bool?;
						return true;
					}
					return false;
				}
			}
			return false;
		}

		// restores original types, lost due to C# compiler optimizations
		// e.g. see https://github.com/linq2db/linq2db/issues/2041
		private static bool RestoreCompare(ref Expression op1, ref Expression op2)
		{
			if (op1.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				var op1conv = (UnaryExpression)op1;

				// handle char replaced with int
				// (int)chr op CONST
				if (op1.Type == typeof(int) && op1conv.Operand.Type == typeof(char)
					&& (op2.NodeType is ExpressionType.Constant or ExpressionType.Convert or ExpressionType.ConvertChecked))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)op2).Operand;
					return true;
				}
				// (int?)chr? op CONST
				else if (op1.Type == typeof(int?) && op1conv.Operand.Type == typeof(char?)
					&& (op2.NodeType == ExpressionType.Constant
						|| (op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)op2).Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)((UnaryExpression)op2).Operand).Operand;
					return true;
				}
				// handle enum replaced with integer
				// here byte/short values replaced with int, int+ values replaced with actual underlying type
				// (int)enum op const
				else if (op1conv.Operand.Type.IsEnum
					&& op2.NodeType == ExpressionType.Constant
						&& (op2.Type == Enum.GetUnderlyingType(op1conv.Operand.Type) || op2.Type == typeof(int)))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(Enum.ToObject(op1conv.Operand.Type, ((ConstantExpression)op2).Value!), op1conv.Operand.Type);
					return true;
				}
				// here underlying type used
				// (int?)enum? op (int?)enum
				else if (op1conv.Operand.Type.IsNullable() && Nullable.GetUnderlyingType(op1conv.Operand.Type)!.IsEnum
					&& op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& op2 is UnaryExpression op2conv2
					&& op2conv2.Operand.NodeType == ExpressionType.Constant
					&& op2conv2.Operand.Type == Nullable.GetUnderlyingType(op1conv.Operand.Type))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Convert(op2conv2.Operand, op1conv.Operand.Type);
					return true;
				}
				// https://github.com/linq2db/linq2db/issues/2039
				// byte, sbyte and ushort comparison operands upcasted to int
				else if (op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& op2 is UnaryExpression op2conv1
					&& op1conv.Operand.Type == op2conv1.Operand.Type
					&& op1conv.Operand.Type != typeof(object))
				{
					op1 = op1conv.Operand;
					op2 = op2conv1.Operand;
					return true;
				}

				// https://github.com/linq2db/linq2db/issues/2166
				// generates expression:
				// Convert(member, int) == const(value, int)
				// we must replace it with:
				// member == const(value, member_type)
				if (op2 is ConstantExpression const2
					&& const2.Type == typeof(int)
					&& ConvertUtils.TryConvert(const2.Value, op1conv.Operand.Type, out var convertedValue))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(convertedValue, op1conv.Operand.Type);
					return true;
				}
			}

			return false;
		}

        #endregion

        #region ConvertEnumConversion

        IAffirmWord? ConvertEnumConversion(IBuildContext context, Expression left, AffirmWord.Operator op, Expression right)
		{
			Expression value;
			Expression operand;

			if (left is MemberExpression)
			{
				operand = left;
				value   = right;
			}
			else if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)left).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else if (right is MemberExpression)
			{
				operand = right;
				value   = left;
			}
			else if (right.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)right).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)right).Operand;
				value   = left;
			}
			else if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else
			{
				operand = ((UnaryExpression)right).Operand;
				value = left;
			}

			var type = operand.Type;

			if (!type.UnwrapNullable().IsEnum)
				return null;

			var dic = new Dictionary<object, object?>();

			//var mapValues = MappingSchema.GetMapValues(type);

			//if (mapValues != null)
			//	foreach (var mv in mapValues)
			//		if (!dic.ContainsKey(mv.OrigValue))
			//			dic.Add(mv.OrigValue, mv.MapValues[0].Value);

			switch (value.NodeType)
			{
				case ExpressionType.Constant:
				{
					var name = Enum.GetName(type, ((ConstantExpression)value).Value!);

					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					// ReSharper disable HeuristicUnreachableCode
					if (name == null)
						return null;
					// ReSharper restore HeuristicUnreachableCode
					// ReSharper restore ConditionIsAlwaysTrueOrFalse

					var origValue = Enum.Parse(type, name, false);

					if (!dic.TryGetValue(origValue, out var mapValue))
						mapValue = origValue;

                        IExpWord l, r;

                        ValueWord sqlvalue=null;
					//var ce = MappingSchema.GetConverter(new DbDataType(type), new DbDataType(typeof(DataParameter)), false, ConversionType.Common);

					//if (ce != null)
					//{
					//	sqlvalue = new ValueWord(ce.ConvertValueToParameter(origValue).Value!);
					//}
					//else
					//{
					//	// TODO: pass column type to type mapValue=null cases?
					//	sqlvalue = DBLive.GetSqlValue(type, mapValue, null);
					//}

					if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
					{
						l = ConvertToSql(context, operand);
						r = sqlvalue;
					}
					else
					{
						r = ConvertToSql(context, operand);
						l = sqlvalue;
					}

					return new ExprExpr(l, op, r, true);
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					value = ((UnaryExpression)value).Operand;

					var cd = SuggestColumnDescriptor(context, operand, value, ProjectFlags.SQL);

					var l = ConvertToSql(context, operand, columnDescriptor: cd);
					var r = ConvertToSql(context, value, columnDescriptor: cd);

					return new ExprExpr(l, op, r, true);
				}
			}

			return null;
		}

		#endregion

		#region ConvertObjectComparison

		static Expression? ConstructMemberPath(MemberInfo[] memberPath, Expression ob, bool throwOnError)
		{
			Expression result = ob;
			foreach (var memberInfo in memberPath)
			{
				if (memberInfo.DeclaringType!.IsAssignableFrom(result.Type))
				{
					result = Expression.MakeMemberAccess(result, memberInfo);
				}
			}

			if (ReferenceEquals(result, ob) && throwOnError)
				throw new LinqToDBException($"Type {result.Type.Name} does not have member {memberPath.Last().Name}.");

			return result;
		}

		#endregion

		#region Parameters

		public static DbDataType GetMemberDataType(DBInstance mappingSchema, MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());


			var col = mappingSchema.client.EntityCash.getEntityInfo(member.ReflectedType!);

			var mem = col?.GetColumn(member);


			var dataType = mem?.DataType;

			if (dataType != null)
				typeResult = typeResult.WithDataType(dataType.Value);

			//var dbType = mem?. ;
			//if (dbType != null)
			//	typeResult = typeResult.WithDbType(dbType);

			if (mem != null && mem.Length != null)
				typeResult = typeResult.WithLength(mem.Length);

			return typeResult;
		}

		private sealed class GetDataTypeContext
		{
			public GetDataTypeContext(DbDataType baseType, DBInstance mappingSchema)
			{
				DataType      = baseType.DataType;
				DbType        = baseType.DbType;
				Length        = baseType.Length;
				Precision     = baseType.Precision;
				Scale         = baseType.Scale;

				MappingSchema = mappingSchema;
			}

			public DataFam      DataType;
			public string?       DbType;
			public int?          Length;
			public int?          Precision;
			public int?          Scale;

			public DBInstance MappingSchema { get; }

		}

		static DbDataType GetDataType(IExpWord expr, DbDataType baseType, DBInstance mappingSchema)
		{
			var ctx = new GetDataTypeContext(baseType, mappingSchema);

			expr.Find(ctx, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ClauseType.SqlField:
					{
						var fld = (FieldWord)e;
						context.DataType     = fld.Type.DataType;
						context.DbType       = fld.Type.DbType;
						context.Length       = fld.Type.Length;
						context.Precision    = fld.Type.Precision;
						context.Scale        = fld.Type.Scale;
						return true;
					}
					case ClauseType.SqlParameter:
					{
						var type             = ((ParameterWord)e).Type;
						context.DataType     = type.DataType;
						context.DbType       = type.DbType;
						context.Length       = type.Length;
						context.Precision    = type.Precision;
						context.Scale        = type.Scale;
						return true;
					}
					case ClauseType.SqlDataType:
					{
						var type             = ((DataTypeWord)e).Type;
						context.DataType     = type.DataType;
						context.DbType       = type.DbType;
						context.Length       = type.Length;
						context.Precision    = type.Precision;
						context.Scale        = type.Scale;
						return true;
					}
					case ClauseType.SqlValue:
					{
						var valueType        = ((ValueWord)e).ValueType;
						context.DataType     = valueType.DataType;
						context.DbType       = valueType.DbType;
						context.Length       = valueType.Length;
						context.Precision    = valueType.Precision;
						context.Scale        = valueType.Scale;
						return true;
					}
					default:
					{
						if (e is IExpWord expr)
						{
							var type = QueryHelper.GetDbDataType(expr,context.MappingSchema);
							context.DataType  = type.DataType;
							context.DbType    = type.DbType;
							context.Length    = type.Length;
							context.Precision = type.Precision;
							context.Scale     = type.Scale;
							return true;
						}
						return false;
					}
				}
			});

			return new DbDataType(
				baseType.SystemType,
				ctx.DataType == DataFam.Undefined ? baseType.DataType : ctx.DataType,
				string.IsNullOrEmpty(ctx.DbType)   ? baseType.DbType   : ctx.DbType,
				ctx.Length     ?? baseType.Length,
				ctx.Precision  ?? baseType.Precision,
				ctx.Scale      ?? baseType.Scale
			);
		}

		#endregion

		#region ConvertInPredicate

		void BuildObjectGetters(SqlGenericConstructorExpression generic, ParameterExpression rootParam, Expression root, List<SqlGetValue> getters)
		{
			for (int i = 0; i < generic.Assignments.Count; i++)
			{
				var assignment = generic.Assignments[i];

				if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
				{
					BuildObjectGetters(subGeneric, rootParam, Expression.MakeMemberAccess(root, assignment.MemberInfo), getters);
				}
				else if (assignment.Expression is SqlPlaceholderExpression placeholder)
				{
					var access = Expression.MakeMemberAccess(root, assignment.MemberInfo);
					var body   = Expression.Convert(access, typeof(object));

					var lambda = Expression.Lambda<Func<object, object>>(body, rootParam);

					getters.Add(new SqlGetValue(placeholder.Sql, placeholder.Type, null, lambda.Compile()));
				}
			}
		}

		private IAffirmWord? ConvertInPredicate(IBuildContext context, MethodCallExpression expression)
		{
			var e        = expression;
			var argIndex = e.Object != null ? 0 : 1;
			var arr      = e.Object ?? e.Arguments[0];
			var arg      = e.Arguments[argIndex];

            IExpWord? expr = null;

			var builtExpr = BuildSqlExpression(context, arg, ProjectFlags.SQL | ProjectFlags.Keys, null);

			if (builtExpr is SqlPlaceholderExpression placeholder)
			{
				expr = placeholder.Sql;
			}
			else if (SequenceHelper.UnwrapDefaultIfEmpty(builtExpr) is SqlGenericConstructorExpression constructor)
			{
				var objParam = Expression.Parameter(typeof(object));

				var getters = new List<SqlGetValue>();
				BuildObjectGetters(constructor, objParam, Expression.Convert(objParam, constructor.ObjectType),
					getters);

				expr = new ObjectWord( getters.ToArray());
			}

			if (expr == null)
				return null;

			var columnDescriptor = QueryHelper.GetColumnDescriptor(expr);

			switch (arr.NodeType)
			{
				case ExpressionType.NewArrayInit :
					{
						var newArr = (NewArrayExpression)arr;

						if (newArr.Expressions.Count == 0)
							return AffirmWord.False;

						var exprs  = new IExpWord[newArr.Expressions.Count];

						for (var i = 0; i < newArr.Expressions.Count; i++)
							exprs[i] = ConvertToSql(context, newArr.Expressions[i], columnDescriptor: columnDescriptor);

						return new data.model.affirms.InList(expr, DBLive.dialect.Option.CompareNullsAsValues ? false : null, false, exprs);
					}

				default :

					if (CanBeCompiled(arr, false))
					{
						var p = ParametersContext.BuildParameter(context, arr, columnDescriptor, forceConstant : false,
							buildParameterType : ParametersContext.BuildParameterType.InPredicate)!.SqlParameter;
						p.IsQueryParameter = false;
						return new InList(expr, DBLive.dialect.Option.CompareNullsAsValues ? false : null, false, p);
					}

					break;
			}

			return null;
		}

		#endregion

		#region ColumnDescriptor Helpers

		public EntityColumn? SuggestColumnDescriptor(IBuildContext? context, Expression expr, ProjectFlags flags)
		{
			expr = expr.Unwrap();

			var converted = ConvertToSqlExpr(context, expr, flags.SqlFlag());
			if (converted is not SqlPlaceholderExpression placeholderTest)
				return null;

			//var descriptor = QueryHelper.GetColumnDescriptor(placeholderTest.Sql);
			//return descriptor;
			return null;
		}

		public EntityColumn? SuggestColumnDescriptor(IBuildContext? context, Expression expr1, Expression expr2, ProjectFlags flags)
		{
			return SuggestColumnDescriptor(context, expr1, flags) ?? SuggestColumnDescriptor(context, expr2, flags);
		}

		public EntityColumn? SuggestColumnDescriptor(IBuildContext? context, ReadOnlyCollection<Expression> expressions, ProjectFlags flags)
		{
			foreach (var expr in expressions)
			{
				var descriptor = SuggestColumnDescriptor(context, expr, flags);
				if (descriptor != null)
					return descriptor;
			}

			return null;
		}

        #endregion

        #region LIKE predicate

        IAffirmWord? CreateStringPredicate(IBuildContext? context, MethodCallExpression expression, mooSQL.data.model.affirms.SearchString.SearchKind kind, IExpWord caseSensitive, ProjectFlags flags)
		{
			var e = expression;

			if (e.Object == null)
				return null;

			var descriptor = SuggestColumnDescriptor(context, e.Object, e.Arguments[0], flags);

			if (!TryConvertToSql(context, e.Object, flags, columnDescriptor : descriptor, sqlExpression : out var o, error : out _))
				return null;

			if (!TryConvertToSql(context, e.Arguments[0], flags, columnDescriptor : descriptor, sqlExpression : out var a, error : out _))
				return null;

			return new mooSQL.data.model.affirms.SearchString(o, false, a, kind, caseSensitive);
		}

        IAffirmWord ConvertLikePredicate(IBuildContext context, MethodCallExpression expression, ProjectFlags flags)
		{
			var e  = expression;

			var descriptor = SuggestColumnDescriptor(context, e.Arguments, flags);

			var a1 = ConvertToSql(context, e.Arguments[0], unwrap: false, columnDescriptor: descriptor);
			var a2 = ConvertToSql(context, e.Arguments[1], unwrap: false, columnDescriptor: descriptor);

            IExpWord? a3 = null;

			if (e.Arguments.Count == 3)
				a3 = ConvertToSql(context, e.Arguments[2], unwrap: false, columnDescriptor: descriptor);

			return new mooSQL.data.model.affirms.Like(a1, false, a2, a3);
		}

		#endregion

		#region MakeIsPredicate

		public IAffirmWord MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
		{
			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return AffirmWord.True;
			}

			return MakeIsPredicate(table, table, table.InheritanceMapping, typeOperand, static (table, name) => table.SqlTable.FindFieldByMemberName(name) ?? throw new LinqException($"Field {name} not found in table {table.SqlTable}"));
		}

		public IAffirmWord MakeIsPredicate<TContext>(
			TContext                              getSqlContext,
			IBuildContext                         context,
			IReadOnlyList<EntiyInherit>     inheritanceMapping,
			Type                                  toType,
			Func<TContext,string, IExpWord> getSql)
		{
			static IAffirmWord CorrectNullability(ExprExpr exprExpr)
			{
				if (exprExpr.Expr2 is ValueWord { Value: null })
				{
					exprExpr.Expr1 = NullabilityWord.ApplyNullability(exprExpr.Expr1, true);
				}
				else if (exprExpr.Expr1 is ValueWord { Value: null })
				{
					exprExpr.Expr2 = NullabilityWord.ApplyNullability(exprExpr.Expr2, true);
				}

				return exprExpr;
			}

			var mapping = new List<EntiyInherit>(inheritanceMapping.Count);
			foreach (var m in inheritanceMapping)
				if (m.Type == toType && !m.IsDefault)
					mapping.Add(m);

			switch (mapping.Count)
			{
				case 0 :
					{
						var cond = new SearchConditionWord();

						var found = false;
						foreach (var m in inheritanceMapping)
						{
							if (m.Type == toType)
							{
								found = true;
								break;
							}
						}

						if (found)
						{
							foreach (var m in inheritanceMapping.Where(static m => !m.IsDefault))
							{
								//cond.Predicates.Add(
								//	CorrectNullability(
								//		new ExprExpr(
								//			getSql(getSqlContext, m.DiscriminatorName),
								//			AffirmWord.Operator.NotEqual,
								//			DBLive.GetSqlValue(m.Entity.MemberType, m.Code, m.Discriminator.GetDbDataType(true)),
								//			DBLive.dialect.Option.CompareNullsAsValues ? true : null)
								//	)
								//);
							}
						}
						else
						{
							var sc = new SearchConditionWord(true);
							foreach (var m in inheritanceMapping)
							{
								if (toType.IsSameOrParentOf(m.Type))
								{
									//sc.Predicates.Add(
									//	CorrectNullability(
									//		new ExprExpr(
									//			getSql(getSqlContext, m.DiscriminatorName),
									//			AffirmWord.Operator.Equal,
         //                                       DBLive.GetSqlValue(m.Discriminator.MemberType, m.Code, m.Discriminator.GetDbDataType(true)),
									//			DBLive.dialect.Option.CompareNullsAsValues ? true : null)
									//	)
									//);
								}
							}

							cond.Add(sc);
						}

						return cond;
					}

				//case 1 :
				//{
				//	//var discriminatorSql = getSql(getSqlContext, mapping[0].Type);
				//	var sqlValue = null;
				//			//DBLive.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code, mapping[0].Discriminator.GetDbDataType(true));

				//	return CorrectNullability(
				//		new ExprExpr(
				//			discriminatorSql,
				//			AffirmWord.Operator.Equal,
				//			sqlValue,
				//			DBLive.dialect.Option.CompareNullsAsValues ? true : null)
				//	);
				//}
				//default:
				//	{
				//		var cond = new SearchConditionWord(true);

				//		foreach (var m in mapping)
				//		{
				//			cond.Predicates.Add(
				//				new ExprExpr(
				//					getSql(getSqlContext, m.DiscriminatorName),
    //                                AffirmWord.Operator.Equal,
    //                                DBLive.GetSqlValue(m.Discriminator.MemberType, m.Code, m.Discriminator.GetDbDataType(true)),
    //                                DBLive.dialect.Option.CompareNullsAsValues ? true : null));
				//		}

				//		return cond;
				//	}
			}
			return null;
		}

        IAffirmWord? MakeIsPredicate(IBuildContext context, TypeBinaryExpression expression, ProjectFlags flags, out SqlErrorExpression? error)
		{
			var predicateExpr = MakeIsPredicateExpression(context, expression);

			return ConvertPredicate(context, predicateExpr, flags, out error);
		}

		Expression MakeIsPredicateExpression(IBuildContext context, TypeBinaryExpression expression)
		{
			var typeOperand = expression.TypeOperand;
			var table       = new TableBuilder.TableContext(this, DBLive, new BuildInfo((IBuildContext?)null, ExpressionInstances.UntypedNull, new SelectQueryClause()), null);

			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return Expression.Constant(true);
			}

			//var mapping = new List<(InheritanceMapping m, int i)>(table.InheritanceMapping.Count);

			//for (var i = 0; i < table.InheritanceMapping.Count; i++)
			//{
			//	var m = table.InheritanceMapping[i];
			//	if (typeOperand.IsAssignableFrom(m.Type) && !m.IsDefault)
			//		mapping.Add((m, i));
			//}

			//var isEqual = true;

			//if (mapping.Count == 0)
			//{
			//	for (var i = 0; i < table.InheritanceMapping.Count; i++)
			//	{
			//		var m = table.InheritanceMapping[i];
			//		if (!m.IsDefault)
			//			mapping.Add((m, i));
			//	}

			//	isEqual = false;
			//}

			Expression? expr = null;

			//foreach (var m in mapping)
			//{
			//	var field = table.SqlTable.FindFieldByMemberName(table.InheritanceMapping[m.i].DiscriminatorName) ?? throw new LinqException($"Field {table.InheritanceMapping[m.i].DiscriminatorName} not found in table {table.SqlTable}");
			//	var ttype = field.ColumnDescriptor.UnderType;
			//	var obj   = expression.Expression;

			//	if (obj.Type != ttype)
			//		obj = Expression.Convert(expression.Expression, ttype);

			//	var memberInfo = ttype.GetMemberEx(field.ColumnDescriptor.PropertyInfo) ?? throw new InvalidOperationException();

			//	var left = Expression.MakeMemberAccess(obj, memberInfo);
			//	var code = m.m.Code;

			//	if (code == null)
			//		code = left.Type.GetDefaultValue();
			//	else if (left.Type != code.GetType())
			//		code = context.Builder.DBLive.dialect.mapping.ChangeTypeTo(code, left.Type);

			//	Expression right = Expression.Constant(code, left.Type);

			//	//var e = isEqual ? Expression.Equal(left, right) : Expression.NotEqual(left, right);

			//	//if (!isEqual)
			//	//	expr = expr != null ? Expression.AndAlso(expr, e) : e;
			//	//else
			//	//	expr = expr != null ? Expression.OrElse(expr, e) : e;
			//}

			return expr!;
		}

		#endregion

		#endregion

		#region Search Condition Builder

		public void BuildSearchCondition(IBuildContext? context, Expression expression, ProjectFlags flags, SearchConditionWord searchCondition)
		{
			if (!BuildSearchCondition(context, expression, flags, searchCondition, out var error))
			{
				throw error.CreateException();
			}
		}

		public bool BuildSearchCondition(IBuildContext? context, Expression expression, ProjectFlags flags, SearchConditionWord searchCondition, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			//expression = GetRemoveNullPropagationTransformer(true).Transform(expression);

			switch (expression.NodeType)
			{
				case ExpressionType.And     :
				case ExpressionType.AndAlso :
				{
					var e           = (BinaryExpression)expression;
					var andCondition = searchCondition.IsAnd ? searchCondition : new SearchConditionWord(false);

					if (!BuildSearchCondition(context, e.Left, flags, andCondition, out var leftError))
					{
						error = leftError;
						return false;
					}

					if (!BuildSearchCondition(context, e.Right, flags, andCondition, out var rightError))
					{
						error = rightError;
						return false;
					}

					if (!searchCondition.IsAnd)
						searchCondition.Add(andCondition);

					break;
				}

				case ExpressionType.Or     :
				case ExpressionType.OrElse :
				{
					var e           = (BinaryExpression)expression;
					var orCondition = searchCondition.IsOr ? searchCondition : new SearchConditionWord(true);

					if (!BuildSearchCondition(context, e.Left, flags, orCondition, out var leftError))
					{
						error = leftError;
						return false;
					}

					if (!BuildSearchCondition(context, e.Right, flags, orCondition, out var rightError))
					{
						error = rightError;
						return false;
					}

					if (!searchCondition.IsOr)
						searchCondition.Add(orCondition);

					break;
				}

				case ExpressionType.Not    :
				{
					var e            = (UnaryExpression)expression;
					var notCondition = new SearchConditionWord();

					if (!BuildSearchCondition(context, e.Operand, flags, notCondition, out error))
						return false;

					searchCondition.Add(notCondition.MakeNot());
					break;
				}

				default                    :
				{
					var predicate = ConvertPredicate(context, expression, flags, out error);

					if (predicate == null)
					{
#pragma warning disable CS8762
						return false;
#pragma warning restore CS8762
					}

					if (predicate is SearchConditionWord sc && (searchCondition.IsOr == sc.IsOr || sc.Predicates.Count <= 1))
					{
						searchCondition.Predicates.AddRange(sc.Predicates);
					}
					else
					{
						searchCondition.Predicates.Add(predicate);
					}

					break;
				}
			}

			error = null;
			return true;
		}

		static bool NeedNullCheck(IExpWord expr)
		{
			if (expr.Find(ClauseType.SelectClause) is null)
				return true;
			return false;
		}

		#endregion

		#region CanBeTranslatedToSql

		private sealed class CanBeTranslatedToSqlContext
		{
			public CanBeTranslatedToSqlContext(ExpressionBuilder builder, IBuildContext buildContext, bool canBeCompiled)
			{
				Builder       = builder;
				BuildContext  = buildContext;
				CanBeCompiled = canBeCompiled;
			}

			public readonly ExpressionBuilder Builder;
			public readonly IBuildContext     BuildContext;
			public readonly bool              CanBeCompiled;
		}

		#endregion

		#region Helpers

		public IBuildContext? GetContext(IBuildContext? current, Expression? expression)
		{
			throw new NotImplementedException();
		}

		internal bool IsNullConstant(Expression expr)
		{
			// TODO: is it correct to return true for DefaultValueExpression for non-reference type or when default value
			// set to non-null value?
			return expr.UnwrapConvert().IsNullValue();
		}

		internal bool IsConstantOrNullValue(Expression expr)
		{
			var unwrapped = expr.UnwrapConvert();
			if (unwrapped is ConstantExpression)
				return true;
			return unwrapped.IsNullValue();
		}

		private TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformer;
		private TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformerForSearch;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TransformVisitor<ExpressionBuilder> GetRemoveNullPropagationTransformer(bool forSearch)
		{
			if (forSearch)
				return _removeNullPropagationTransformerForSearch ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, true));
			else
				return _removeNullPropagationTransformer ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, false));
		}

		public Expression RemoveNullPropagation(IBuildContext context, Expression expr, ProjectFlags flags, bool toSql)
		{
			static bool? IsNull(Expression sqlExpr)
			{
				if (sqlExpr.IsNullValue())
					return true;

				if (sqlExpr is not SqlPlaceholderExpression placeholder)
					return null;

				return QueryHelper.IsNullValue(placeholder.Sql);
			}

			if (expr.NodeType == ExpressionType.Equal || expr.NodeType == ExpressionType.NotEqual)
			{
				var binary = (BinaryExpression)expr;

				var left  = RemoveNullPropagation(context, binary.Left, flags, true);
				var right = RemoveNullPropagation(context, binary.Right, flags, true);

				if (toSql)
				{
					binary = binary.Update(
						left,
						binary.Conversion,
						right);
				}

				return binary;
			}

			if (expr.NodeType == ExpressionType.Conditional)
			{
				var cond = (ConditionalExpression)expr;

				var test    = RemoveNullPropagation(context, cond.Test, flags, true);
				var ifTrue  = RemoveNullPropagation(context, cond.IfTrue, flags, true);
				var ifFalse = RemoveNullPropagation(context, cond.IfFalse, flags, true);

				if (test.NodeType == ExpressionType.Equal || test.NodeType == ExpressionType.NotEqual)
				{
					var testLeft  = ((BinaryExpression)test).Left;
					var testRight = ((BinaryExpression)test).Right;

					var nullLeft  = IsNull(testLeft);
					var nullRight = IsNull(testRight);

					if (nullRight == true && nullLeft == true)
					{
						return test.NodeType == ExpressionType.Equal ? cond.IfTrue : cond.IfFalse;
					}

					if (test.NodeType == ExpressionType.Equal)
					{
						if (IsNull(ifTrue) == true && (nullLeft == true || nullRight == true))
						{
							return toSql ? ifFalse : cond.IfFalse;
						}
					}
					else
					{
						if (IsNull(ifFalse) == true && (nullLeft == true || nullRight == true))
						{
							return toSql ? ifTrue : cond.IfTrue;
						}
					}
				}

				if (toSql)
				{
					cond = cond.Update(test, ifTrue, ifFalse);
				}

				return cond;
			}

			var doNotConvert = 
				expr.NodeType is ExpressionType.Equal
				              or ExpressionType.NotEqual
				              or ExpressionType.GreaterThan
				              or ExpressionType.GreaterThanOrEqual
				              or ExpressionType.LessThan
				              or ExpressionType.LessThanOrEqual;

			if (!doNotConvert && toSql)
			{
				var sql = ConvertToSqlExpr(context, expr, flags);
				if (sql is SqlPlaceholderExpression or SqlGenericConstructorExpression)
					return sql;
			}
			return expr;
		}

		public Expression RemoveNullPropagation(Expression expr, bool forSearch)
		{
			bool IsAcceptableType(Type type)
			{
				if (!forSearch)
					return type.IsReferType();

				if (type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type))
					return true;

				if (!DBLive.dialect.mapping.IsScalarType(type))
					return true;

				return false;
			}

			// Do not modify parameters
			//
			if (CanBeCompiled(expr, false))
				return expr;

			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
							else if (IsNullConstant(conditional.IfFalse)
								&& ((nullRight && IsAcceptableType(binary.Left.Type) ||
									(nullLeft  && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
							else if (IsNullConstant(conditional.IfTrue)
									 && ((nullRight && IsAcceptableType(binary.Left.Type) ||
										  (nullLeft && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
						}
					}
					break;
			}

			return expr;
		}

		public bool ProcessProjection(Dictionary<MemberInfo,Expression> members, Expression expression)
		{
			void CollectParameters(Type forType, MethodBase method, ReadOnlyCollection<Expression> arguments)
			{
				var pms = method.GetParameters();

				var typeMembers = TypeAccessor.GetAccessor(forType).Members;

				for (var i = 0; i < pms.Length; i++)
				{
					var param = pms[i];
					MemberAccessor? foundMember = null;
					foreach (var tm in typeMembers)
					{
						if (tm.Name == param.Name)
						{
							foundMember = tm;
							break;
						}
					}

					if (foundMember == null)
					{
						foreach (var tm in typeMembers)
						{
							if (tm.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
							{
								foundMember = tm;
								break;
							}
						}
					}

					if (foundMember == null)
						continue;

					if (members.ContainsKey(foundMember.MemberInfo))
						continue;

					var converted = arguments[i];

					members.Add(foundMember.MemberInfo, converted);
				}
			}

			expression = GetRemoveNullPropagationTransformer(false).Transform(expression);

			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

						if (expr.Members != null)
						{
							for (var i = 0; i < expr.Members.Count; i++)
							{
								var member = expr.Members[i];

								var converted = expr.Arguments[i];
								members.Add(member, converted);

								if (member is MethodInfo info)
									members.Add(info.GetPropertyInfo(), converted);
							}
						}

						var isScalar =DBLive.dialect.mapping .IsScalarType(expr.Type);
						if (!isScalar)
							CollectParameters(expr.Type, expr.Constructor!, expr.Arguments);

						return members.Count > 0 || !isScalar;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr        = (MemberInitExpression)expression;
						var typeMembers = TypeAccessor.GetAccessor(expr.Type).Members;

						var dic  = typeMembers
							.Select(static (m,i) => new { m, i })
							.ToDictionary(static _ => _.m.MemberInfo.Name, static _ => _.i);

						var assignments = new List<(MemberAssignment ma, int order)>();
						foreach (var ma in expr.Bindings.Cast<MemberAssignment>())
							assignments.Add((ma, dic.TryGetValue(ma.Member.Name, out var idx) ? idx : 1000000));

						foreach (var (binding, _) in assignments.OrderBy(static a => a.order))
						{
							var converted = binding.Expression;
							members.Add(binding.Member, converted);

							if (binding.Member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), converted);
						}

						return true;
					}

				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expression;

						// process fabric methods

						if (!DBLive.dialect.mapping.IsScalarType(mc.Type))
							CollectParameters(mc.Type, mc.Method, mc.Arguments);

						return members.Count > 0;
					}

				case ExpressionType.NewArrayInit:
				case ExpressionType.ListInit:
					{
						return true;
					}
				// .Select(p => everything else)
				//
				default                        :
					return false;
			}
		}

		#endregion

		#region CTE

		Dictionary<Expression, CteContext>? _cteContexts;

		public void RegisterCteContext(CteContext cteContext, Expression cteExpression)
		{
			_cteContexts ??= new(ExpressionEqualityComparer.Instance);

			_cteContexts.Add(cteExpression, cteContext);
		}

		public CteContext? FindRegisteredCteContext(Expression cteExpression)
		{
			if (_cteContexts == null)
				return null;

			_cteContexts.TryGetValue(cteExpression, out var cteContext);

			return cteContext;
		}

		#endregion

		#region Query Filter

		private Stack<Type[]>? _disabledFilters;

		public void PushDisabledQueryFilters(Type[] disabledFilters)
		{
			_disabledFilters ??= new Stack<Type[]>();
			_disabledFilters.Push(disabledFilters);
		}

		public bool IsFilterDisabled(Type entityType)
		{
			if (_disabledFilters == null || _disabledFilters.Count == 0)
				return false;
			var filter = _disabledFilters.Peek();
			if (filter.Length == 0)
				return true;
			return Array.IndexOf(filter, entityType) >= 0;
		}

		public void PopDisabledFilter()
		{
			if (_disabledFilters == null)
				throw new InvalidOperationException();

			_ = _disabledFilters.Pop();
		}

		#endregion

		#region Query Hint Stack

		List<QueryExtension>? _sqlQueryExtensionStack;

		public void PushSqlQueryExtension(QueryExtension extension)
		{
			(_sqlQueryExtensionStack ??= new()).Add(extension);
		}

		public void PopSqlQueryExtension(QueryExtension extension)
		{
			if (_sqlQueryExtensionStack == null || _sqlQueryExtensionStack.Count > 0)
				throw new InvalidOperationException();
			_sqlQueryExtensionStack.RemoveAt(_sqlQueryExtensionStack.Count - 1);
		}

		#endregion

		#region Grouping Guard

		public bool IsGroupingGuardDisabled { get; set; }

		#endregion

		#region Projection

		public Expression Project(IBuildContext context, Expression? path, List<Expression>? nextPath, int nextIndex, ProjectFlags flags, Expression body, bool strict)
		{
			MemberInfo? member = null;
			Expression? next   = null;

			if (path is MemberExpression memberExpression)
			{
				nextPath ??= new();
				nextPath.Add(memberExpression);

				if (memberExpression.Expression is MemberExpression me)
				{
					// going deeper
					return Project(context, me, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				if (memberExpression.Expression!.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					// going deeper
					return Project(context, ((UnaryExpression)memberExpression.Expression).Operand, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path is SqlGenericParamAccessExpression accessExpression)
			{
				nextPath ??= new();
				nextPath.Add(accessExpression);

				if (accessExpression.Constructor is SqlGenericParamAccessExpression ae)
				{
					// going deeper
					return Project(context, ae, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path == null)
			{
				if (nextPath == null || nextIndex < 0)
				{
					if (body == null)
						throw new InvalidOperationException();

					return body;
				}

				next = nextPath[nextIndex];

				if (next is MemberExpression me)
				{
					member = me.Member;
				}
				else if (next is SqlGenericParamAccessExpression paramAccess)
				{
					if (body.NodeType == ExpressionType.New)
					{
						var newExpr = (NewExpression)body;
						if (newExpr.Constructor == paramAccess.ParameterInfo.Member && paramAccess.ParamIndex < newExpr.Arguments.Count)
						{
							return Project(context, null, nextPath, nextIndex - 1, flags,
								newExpr.Arguments[paramAccess.ParamIndex], strict);
						}
					}
					else if (body.NodeType == ExpressionType.Call)
					{
						var methodCall = (MethodCallExpression)body;
						if (methodCall.Method == paramAccess.ParameterInfo.Member && paramAccess.ParamIndex < methodCall.Arguments.Count)
						{
							return Project(context, null, nextPath, nextIndex - 1, flags,
								methodCall.Arguments[paramAccess.ParamIndex], strict);
						}
					}

					// nothing to do right now
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (flags.HasFlag(ProjectFlags.SQL))
			{
				body = RemoveNullPropagation(body, flags.HasFlag(ProjectFlags.Keys));
			}

			if (body is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				body = defaultIfEmptyExpression.InnerExpression;
			}

			switch (body.NodeType)
			{
				case ExpressionType.Extension:
				{
					if (body is SqlPlaceholderExpression placeholder)
					{
						return placeholder;
					}

					if (member != null)
					{
						if (body is ContextRefExpression contextRef)
						{
							var objExpression   = body;
							var memberCorrected = contextRef.Type.GetMemberEx(member);
							if (memberCorrected  is null)
							{
								// inheritance handling
								if (member.DeclaringType != null &&
									contextRef.Type.IsSameOrParentOf(member.DeclaringType))
								{
									memberCorrected = member;
									objExpression   = Expression.Convert(objExpression, member.DeclaringType);
								}
								else
								{
									return next!;
								}
							}

							var ma      = Expression.MakeMemberAccess(objExpression, memberCorrected);
							var newPath = nextPath![0].Replace(next!, ma);

							return newPath;
						}

						if (body.IsNullValue())
						{
							return new DefaultValueExpression(DBLive, member.GetMemberType());
						}

						if (body is SqlGenericConstructorExpression genericConstructor)
						{
							Expression? bodyExpresion = null;
							for (int i = 0; i < genericConstructor.Assignments.Count; i++)
							{
								var assignment = genericConstructor.Assignments[i];
								if (MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, member))
								{
									bodyExpresion = assignment.Expression;
									break;
								}
							}

							if (bodyExpresion == null)
							{
								for (int i = 0; i < genericConstructor.Parameters.Count; i++)
								{
									var parameter = genericConstructor.Parameters[i];
									if (MemberInfoEqualityComparer.Default.Equals(parameter.MemberInfo, member))
									{
										bodyExpresion = parameter.Expression;
										break;
									}
								}
							}

							if (bodyExpresion == null)
							{
								// search in base class
								for (int i = 0; i < genericConstructor.Assignments.Count; i++)
								{
									var assignment = genericConstructor.Assignments[i];
									if (assignment.MemberInfo.ReflectedType != member.ReflectedType && assignment.MemberInfo.Name == member.Name)
									{
										var mi = assignment.MemberInfo.ReflectedType!.GetMemberEx(member);
										if (mi != null && MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, mi))
										{
											bodyExpresion = assignment.Expression;
											break;
										}
									}
								}
							}

							if (bodyExpresion is not null)
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, bodyExpresion, strict);
							}

							if (strict)
								return CreateSqlError(null, nextPath![0]);

							return new DefaultValueExpression(DBLive, nextPath![0].Type);
						}
					}

					if (next is SqlGenericParamAccessExpression paramAccessExpression)
					{

						/*
						var projected = Project(context, path, nextPath, nextIndex - 1, flags,
							paramAccessExpression);

						return projected;
						*/

						if (body is SqlGenericConstructorExpression constructorExpression)
						{
							var projected = Project(context, path, nextPath, nextIndex - 1, flags,
								constructorExpression.Parameters[paramAccessExpression.ParamIndex].Expression, strict);
							return projected;
						}

						//throw new InvalidOperationException();
					}

					return body;
				}

				case ExpressionType.MemberAccess:
				{
					if (member != null && nextPath != null)
					{
						if (nextPath[nextIndex] is MemberExpression nextMember && body.Type.IsSameOrParentOf(nextMember.Expression!.Type))
						{
							var newMember = body.Type.GetMemberEx(nextMember.Member);
							if (newMember != null)
							{
								var newMemberAccess = Expression.MakeMemberAccess(body, newMember);
								return Project(context, path, nextPath, nextIndex - 1, flags, newMemberAccess, strict);
							}
						}
					}

					break;
				}

				case ExpressionType.New:
				{
					var ne = (NewExpression)body;

					if (ne.Members != null)
					{
						if (member == null)
						{
							break;
						}

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								var projected = Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);

								// set alias
								if (projected is ContextRefExpression contextRef)
								{
									contextRef.BuildContext.SetAlias(member.Name);
								}

								return projected;
							}
						}
					}
					else
					{
						var parameters = ne.Constructor!.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType!, parameter);

							if (memberByParam != null &&
								MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					if (member == null)
						return ne;

					if (strict)
						return CreateSqlError(null, nextPath![0]);

					return new DefaultValueExpression(DBLive, nextPath![0].Type);
				}

				case ExpressionType.MemberInit:
				{
					var mi = (MemberInitExpression)body;
					var ne = mi.NewExpression;

					if (member == null)
					{
						if (next is SqlGenericParamAccessExpression paramAccess)
						{
							if (paramAccess.ParamIndex >= ne.Arguments.Count)
								return CreateSqlError(context, nextPath![0]);

							return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[paramAccess.ParamIndex], strict);
						}

						throw new NotImplementedException($"Projecting '{next}' is not supported yet.");
					}

					if (ne.Members != null)
					{

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					var memberInType = body.Type.GetMemberEx(member);
					if (memberInType == null)
					{
						if (member.DeclaringType?.IsSameOrParentOf(body.Type) == true)
							memberInType = member;
					}

					if (memberInType != null)
					{
						for (int index = 0; index < mi.Bindings.Count; index++)
						{
							var binding = mi.Bindings[index];
							switch (binding.BindingType)
							{
								case MemberBindingType.Assignment:
								{
									var assignment = (MemberAssignment)binding;
									if (MemberInfoEqualityComparer.Default.Equals(assignment.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											assignment.Expression, strict);
									}

									break;
								}
								case MemberBindingType.MemberBinding:
								{
									var memberMemberBinding = (MemberMemberBinding)binding;
									if (MemberInfoEqualityComparer.Default.Equals(memberMemberBinding.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											new SqlGenericConstructorExpression(
												memberMemberBinding.Member.GetMemberType(),
												memberMemberBinding.Bindings), strict);
									}

									break;
								}
								case MemberBindingType.ListBinding:
									throw new NotImplementedException();
								default:
									throw new NotImplementedException();
							}
						}

						if (ne.Constructor != null && ne.Arguments.Count > 0)
						{
							var parameters = ne.Constructor.GetParameters();
							for (int i = 0; i < ne.Arguments.Count; i++)
							{
								var parameter     = parameters[i];
								var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType!, parameter);

								if (memberByParam != null &&
									MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
								{
									return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
								}

							}
						}
					}

					if (strict)
						return CreateSqlError(null, nextPath![0]);

					return new DefaultValueExpression(DBLive, nextPath![0].Type);

				}
				case ExpressionType.Conditional:
				{
					var cond      = (ConditionalExpression)body;
					var trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, strict);
					var falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, strict);

					var trueHasError = trueExpr is SqlErrorExpression;
					var falseHasError = falseExpr is SqlErrorExpression;

					if (strict && (trueHasError || falseHasError))
					{
						if (trueHasError == falseHasError)
						{
							return trueExpr;
						}

						trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, false);
						falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, false);
					}

					if (trueExpr is SqlErrorExpression || falseExpr is SqlErrorExpression)
					{
						break;
					}

					if (trueExpr.Type != falseExpr.Type)
					{
						if (trueExpr.IsNullValue())
							trueExpr = new DefaultValueExpression(DBLive, falseExpr.Type);
						else if (falseExpr.IsNullValue())
							falseExpr = new DefaultValueExpression(DBLive, trueExpr.Type);
					}

					var newExpr = (Expression)Expression.Condition(cond.Test, trueExpr, falseExpr);

					return newExpr;
				}

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						/*
						if (expr.Type.IsValueType)
						{
							var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);
							return placeholder;
						}
						*/

						return new DefaultValueExpression(DBLive, expr.Type);
					}

					break;

				}
				case ExpressionType.Default:
				{
					var expr = (path ?? next)!;

					/*
					if (expr.Type.IsValueType)
					{
						var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);
						return placeholder;
					}
					*/

					return new DefaultValueExpression(DBLive, expr.Type);
				}

				/*
				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						var placeholder = CreatePlaceholder(context, new SqlValue(expr.Type, null), expr);

						return placeholder;
					}

					return body;
				}

				case ExpressionType.Default:
				{
					var placeholder = CreatePlaceholder(context, new SqlValue(body.Type, null), body);
					return placeholder;
				}
				*/

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)body;

					if (mc.Method.IsStatic)
					{
						var parameters = mc.Method.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(mc.Method.ReturnType, parameter);

							if (memberByParam != null &&
								MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, mc.Arguments[i], strict);
							}
						}
					}

					if (member != null)
					{
						var ma = Expression.MakeMemberAccess(mc, member);
						return Project(context, path, nextPath, nextIndex - 1, flags, ma, strict);
					}

					return mc;
				}

				case ExpressionType.TypeAs:
				{
					var unary = (UnaryExpression)body;

					var truePath = Project(context, path, nextPath, nextIndex, flags, unary.Operand, strict);

					var isPredicate = MakeIsPredicateExpression(context, Expression.TypeIs(unary.Operand, unary.Type));

					if (isPredicate is ConstantExpression constExpr)
					{
						if (constExpr.Value is true)
							return truePath;
						return new DefaultValueExpression(DBLive, truePath.Type);
					}

					var falsePath = Expression.Constant(null, truePath.Type);

					var conditional = Expression.Condition(isPredicate, truePath, falsePath);

					return conditional;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var unaryExpression = (UnaryExpression)body;

					if (unaryExpression.Operand is ContextRefExpression contextRef)
					{
						contextRef = contextRef.WithType(unaryExpression.Type);
						return Project(context, path, nextPath, nextIndex, flags, contextRef, strict);
					}

					break;
				}
			}

			return CreateSqlError(context, next!);
		}

		public Expression ParseGenericConstructor(Expression createExpression, ProjectFlags flags, EntityColumn? columnDescriptor, bool force = false)
		{
			if (createExpression.Type.IsNullable())
				return createExpression;

			if (!force && createExpression.Type.IsValueType)
				return createExpression;

			if (!force && DBLive.dialect.mapping.IsScalarType(createExpression.Type))
				return createExpression;

			if (typeof(FormattableString).IsSameOrParentOf(createExpression.Type))
				return createExpression;

			if (flags.IsSql() && IsForceParameter(createExpression, columnDescriptor))
				return createExpression;

			switch (createExpression.NodeType)
			{
				case ExpressionType.New:
				{
					return new SqlGenericConstructorExpression((NewExpression)createExpression);
				}

				case ExpressionType.MemberInit:
				{
					return new SqlGenericConstructorExpression((MemberInitExpression)createExpression);
				}

				case ExpressionType.Call:
				{
					//TODO: Do we still need Alias?
					var mc = (MethodCallExpression)createExpression;
					if (mc.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
						return ParseGenericConstructor(mc.Arguments[0], flags, columnDescriptor);

					if (mc.IsQueryable())
						return mc;

					if (!mc.Method.IsStatic)
						break;

					if (mc.Method.IsSqlPropertyMethodEx() || mc.IsSqlRow() || mc.Method.DeclaringType == typeof(string))
						break;

					return new SqlGenericConstructorExpression(mc);
				}
			}

			return createExpression;
		}

		Dictionary<SqlCacheKey, Expression>                  _expressionCache    = new(SqlCacheKey.SqlCacheKeyComparer);
		Dictionary<ColumnCacheKey, SqlPlaceholderExpression> _columnCache = new(ColumnCacheKey.ColumnCacheKeyComparer);

#if DEBUG
		int _makeCounter;
#endif

		/// <summary>
		/// 缓存创建的表达式
		/// </summary>
		/// <param name="forContext"></param>
		/// <param name="path"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public Expression MakeExpression(IBuildContext? forContext, Expression path, ProjectFlags flags)
		{
			//迁移走
			throw new NotImplementedException();
			//return expression;
		}

		public bool IsSimpleForCompilation(IBuildContext context, Expression expr)
		{
			if (CanBeConstant(expr))
				return true;
			var sqlExpr = ConvertToSqlExpr(context, expr, ProjectFlags.SQL | ProjectFlags.Test);
			return sqlExpr is SqlPlaceholderExpression || ExpressionEqualityComparer.Instance.Equals(sqlExpr, expr);
		}

		public SqlPlaceholderExpression MakeColumn(SelectQueryClause? parentQuery, SqlPlaceholderExpression sqlPlaceholder, bool asNew = false)
		{
			if (parentQuery == sqlPlaceholder.SelectQuery)
				throw new InvalidOperationException();

			var placeholderType = sqlPlaceholder.Type;
			if (placeholderType.IsNullable())
				placeholderType = placeholderType.UnwrapNullableType();

			if (sqlPlaceholder.SelectQuery == null)
				throw new InvalidOperationException($"Placeholder with path '{sqlPlaceholder.Path}' and SQL '{sqlPlaceholder.Sql}' has no SelectQuery defined.");

			var key = new ColumnCacheKey(sqlPlaceholder.Path, placeholderType, sqlPlaceholder.SelectQuery, parentQuery);

			if (!asNew && _columnCache.TryGetValue(key, out var placeholder))
			{
				return placeholder.WithType(sqlPlaceholder.Type);
			}

			var alias = sqlPlaceholder.Alias;

			if (string.IsNullOrEmpty(alias))
			{
				if (sqlPlaceholder.TrackingPath is MemberExpression tme)
					alias = tme.Member.Name;
				else if (sqlPlaceholder.Path is MemberExpression me)
					alias = me.Member.Name;
			}

			/*

			// Left here for simplifying debugging

			var findStr = "Ref(TableContext[ID:1](13)(T: 14)::ElementTest).Id";
			if (sqlPlaceholder.Path.ToString().Contains(findStr))
			{
				var found = _columnCache.Keys.FirstOrDefault(c => c.Expression?.ToString().Contains(findStr) == true);
				if (found.Expression != null)
				{
					if (_columnCache.TryGetValue(found, out var current))
					{
						var fh = ExpressionEqualityComparer.Instance.GetHashCode(found.Expression);
						var kh = ExpressionEqualityComparer.Instance.GetHashCode(key.Expression);

						var foundHash = ColumnCacheKey.ColumnCacheKeyComparer.GetHashCode(found);
						var KeyHash   = ColumnCacheKey.ColumnCacheKeyComparer.GetHashCode(key);
					}
				}
			}

			*/

			var sql    = sqlPlaceholder.Sql;
			var idx    = sqlPlaceholder.SelectQuery.Select.AddNew(sql);
			var column = sqlPlaceholder.SelectQuery.Select.Columns[idx];

			if (!string.IsNullOrEmpty(alias))
			{
				column.RawAlias = alias;
			}

			placeholder = CreatePlaceholder(parentQuery, column, sqlPlaceholder.Path, sqlPlaceholder.ConvertType, alias, idx, trackingPath: sqlPlaceholder.TrackingPath);

			_columnCache[key] = placeholder;

			return placeholder;
		}

		#endregion
	}
}
