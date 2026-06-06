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

    partial class ClauseSqlTranslator
	{
		#region BuildExpression

		public Expression ConvertToExtensionSql(IClauseContext context, ProjectFlags flags, Expression expression, EntityColumn? columnDescriptor, bool? inlineParameters)
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
			public SqlCacheKey(Expression? expression, IClauseContext? context, EntityColumn? columnDescriptor, SelectQueryClause? selectQuery, ProjectFlags flags)
			{
				Expression       = expression;
				Context          = context;
				ColumnDescriptor = columnDescriptor;
				SelectQuery      = selectQuery;
				Flags            = flags;
			}

			public Expression?       Expression       { get; }
			public IClauseContext?    Context          { get; }
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

		public SqlPlaceholderExpression ConvertToSqlPlaceholder(IClauseContext? context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, bool forceParameter = false)
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

		public IExpWord ConvertToSql(IClauseContext? context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, bool forceParameter = false)
		{
			var placeholder = ConvertToSqlPlaceholder(context, expression, flags, unwrap : unwrap,
				columnDescriptor : columnDescriptor, isPureExpression : isPureExpression, forExtension : forExtension,
				forceParameter : forceParameter);

			return placeholder.Sql;
		}
        public IExpWord ConvertToSqlEn(IClauseContext? context, Expression expression, ProjectFlags flags = ProjectFlags.SQL, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, bool forceParameter = false)
        {
            var placeholder = ConvertToSqlPlaceholder(context, expression, flags, unwrap: unwrap,
                columnDescriptor: null, isPureExpression: isPureExpression, forExtension: forExtension,
                forceParameter: forceParameter);

            return placeholder.Sql;
        }

        public static SqlPlaceholderExpression CreatePlaceholder(IClauseContext? context, IExpWord sqlExpression,
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
		public Expression ConvertToSqlExpr(IClauseContext? context, Expression expression,
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

			newExpr = BuildProjection(context, newExpr, flags);

			if (newExpr is SqlErrorExpression)
				return newExpr;

			var noConvert = newExpr.UnwrapConvert();
			if (typeof(IExpWord).IsSameOrParentOf(newExpr.Type) || typeof(IExpWord).IsSameOrParentOf(noConvert.Type))
			{
				var valid = true;
				if (newExpr is MethodCallExpression mc)
				{
					var type = mc.Object?.Type ?? mc.Method.DeclaringType;
					//if (type != null && MappingSchema.HasAttribute<DbFunc.ExpressionAttribute>(type, mc.Method))
					//	valid = false;
				}
				else if (newExpr is MemberExpression me)
				{
					var type = me.Expression?.Type ?? me.Member.DeclaringType;
					//if (type != null && MappingSchema.HasAttribute<DbFunc.ExpressionAttribute>(type, me.Member))
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

		Expression ConvertToSqlInternal(IClauseContext? context, Expression expression, ProjectFlags flags, bool unwrap = false, EntityColumn? columnDescriptor = null, bool isPureExpression = false, bool forExtension = false, string? alias = null)
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
						// Otherwise should be handled by BuildProjection
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

					if (mc.IsSameGenericMethod(Methods.SooQuery.SqlExt.Alias))
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

		public Expression? TranslateMember(IClauseContext? context, ProjectFlags flags, EntityColumn? columnDescriptor, string? alias, Expression memberExpression)
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

		bool IsAlreadyTranslated(IClauseContext? context, ProjectFlags flags, EntityColumn? columnDescriptor, Expression memberExpression, out SqlCacheKey cacheKey, [NotNullWhen(true)] out Expression? translatedExpression)
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

		public IExpWord? TryConvertFormatToSql(IClauseContext? context, MethodCallExpression mc, bool isPureExpression, ProjectFlags flags)
		{
			// TODO: move PrepareRawSqlArguments to more correct location
			TableRawSqlHelper.PrepareRawSqlArguments(mc, null,
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

		public Expression ConvertExtensionToSql(IClauseContext context, ProjectFlags flags, DbFunc.ExpressionAttribute attr, MethodCallExpression mc, bool checkAggregateRoot)
		{




			var currentContext = context;

			if ((attr.IsAggregate || attr.IsWindowFunction) && checkAggregateRoot)
			{
				var sequenceRef = new ContextRefExpression(context.ElementType, context);

				var rootContext = GetRootContext(context, sequenceRef, true);

				currentContext = rootContext?.BuildContext ?? currentContext;

				if (currentContext is GroupByContext groupCtx)
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

        IExpWord? ConvertToInlinedSqlExpression(IClauseContext? context, Expression newExpr)
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

		public IExpWord? ConvertToSqlConvertible(IClauseContext? context, Expression expression)
		{
			if (EvaluateExpression(Expression.Convert(expression, typeof(IToSqlConverter))) is not IToSqlConverter converter)
				throw new SooQueryException($"Expression '{expression}' cannot be converted to `IToSqlConverter`");

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
	}
}
