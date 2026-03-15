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

    partial class ExpressionBuilder
	{
		#region BuildExpression


        Expression FinalizeProjection(
			IBuildContext context,
			Expression expression,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[] previousKeys)
        {
            // 非查询 表达式 快速返回
            if (expression.NodeType == ExpressionType.Default)
                return expression;

            // 转换所有遗漏的引用

            var postProcessed = FinalizeConstructors(context, expression, true);

            // 处理急切加载查询
            var correctedEager = CompleteEagerLoadingExpressions(postProcessed, context, queryParameter, ref preambles, previousKeys);

            if (SequenceHelper.HasError(correctedEager))
                return correctedEager;

            if (!ExpressionEqualityComparer.Instance.Equals(correctedEager, postProcessed))
            {
                // convert all missed references
                postProcessed = FinalizeConstructors(context, correctedEager, false);
            }

            var withColumns = ToColumns(context, postProcessed);
            return withColumns;
        }

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

		public TExpression UpdateNesting<TExpression>(IBuildContext upToContext, TExpression expression)
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

		public Expression ToColumns(IBuildContext rootContext, Expression expression)
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
									throw new LinqToDBException("Sql.Row(...) cannot be top level expression.");
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

		public bool TryConvertToSql(IBuildContext? context, Expression expression, ProjectFlags flags,
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

		public SqlPlaceholderExpression? TryConvertToSqlPlaceholder(IBuildContext context, Expression expression, ProjectFlags flags, EntityColumn? columnDescriptor = null)
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

		public static SqlErrorExpression CreateSqlError(IBuildContext? context, Expression expression)
		{
			return new SqlErrorExpression(context, expression);
		}

		public static bool HasError(Expression expression)
		{
			return null != expression.Find(0, (_, e) => e is SqlErrorExpression);
		}

		public Expression ConvertExtension(Sql.ExpressionAttribute attr, IBuildContext context, Expression expr, ProjectFlags flags)
		{
			var rootContext     = context;
			var rootSelectQuery = context.SelectQuery;

			var root = GetRootContext(context.Parent, new ContextRefExpression(context.ElementType, context), true);
			if (root != null)
			{
				rootContext = root.BuildContext;
			}

			if (rootContext is GroupByBuilder.GroupByContext groupBy)
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

		public Expression HandleExtension(IBuildContext context, Expression expr, ProjectFlags flags)
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

		public Expression FinalizeConstructors(IBuildContext context, Expression inputExpression, bool deduplicate)
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
			public IBuildContext? Context;
			public bool           IsSequence;
		}

		public Expression CorrectRoot(IBuildContext? currentContext, Expression expr)
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
			else if (expr is ContextRefExpression { BuildContext: DefaultIfEmptyBuilder.DefaultIfEmptyContext di })
			{
				return CorrectRoot(di.Sequence, new ContextRefExpression(expr.Type, di.Sequence));
			}

			var newExpr = MakeExpression(currentContext, expr, ProjectFlags.Traverse);
			if (!ExpressionEqualityComparer.Instance.Equals(newExpr, expr))
			{
				newExpr = CorrectRoot(currentContext, newExpr);
			}

			return newExpr;
		}

		public ContextRefExpression? GetRootContext(IBuildContext? currentContext, Expression? expression, bool isAggregation)
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
				var newExpression = MakeExpression(currentContext, expression, isAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root);

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






		static string [] _singleElementMethods =
		{
			nameof(Enumerable.FirstOrDefault),
			nameof(Enumerable.First),
			nameof(Enumerable.Single),
			nameof(Enumerable.SingleOrDefault),
		};



		#endregion

		#region PreferServerSide

		private FindVisitor<ExpressionBuilder>? _enforceServerSideVisitorTrue;
		private FindVisitor<ExpressionBuilder>? _enforceServerSideVisitorFalse;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private FindVisitor<ExpressionBuilder> GetVisitor(bool enforceServerSide)
		{
			if (enforceServerSide)
				return _enforceServerSideVisitorTrue ??= FindVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, true));
			else
				return _enforceServerSideVisitorFalse ??= FindVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, false));
		}

		public bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			if (expr.Type == typeof(Sql.SqlID))
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

		#region Build Mapper

		public Expression ToReadExpression(
			ExpressionGenerator expressionGenerator,
			NullabilityContext  nullability,
			Expression          expression)
		{
			Expression? rowCounter = null;

			var simplified = expression.Transform(e =>
			{
				if (e.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && e.Type != typeof(object))
				{
					if (((UnaryExpression)e).Operand is SqlPlaceholderExpression convertPlaceholder)
					{
						return convertPlaceholder.WithType(e.Type);
					}
				}

				return e;
			});

			var toRead = simplified.Transform(e =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (placeholder.Sql == null)
						throw new InvalidOperationException();
					if (placeholder.Index == null)
						throw new InvalidOperationException();
					//字段描述信息
					var columnDescriptor = QueryHelper.GetColumnDescriptor(placeholder.Sql);
					//字段值类型
					var valueType = columnDescriptor?.DbType.SystemType
					                ?? placeholder.Type;
					//可空
					var canBeNull = nullability.CanBeNull(placeholder.Sql) || placeholder.Type.IsNullable();

					if (canBeNull && valueType != placeholder.Type && valueType.IsValueType && !valueType.IsNullable())
					{
						valueType = valueType.WrapNullable();
					}
					// 可空的包裹类型
					if (placeholder.Type != valueType && valueType.IsNullable() && placeholder.Type == valueType.UnwrapNullable())
					{
						// let ConvertFromDataReaderExpression handle default value
						valueType = placeholder.Type;
					}

					var readerExpression = (Expression)new ConvertFromDataReaderExpression(valueType, placeholder.Index.Value,
						null, DataReaderParam, canBeNull);

					if (placeholder.Type != readerExpression.Type)
					{
						readerExpression = Expression.Convert(readerExpression, placeholder.Type);
					}

					return new TransformInfo(readerExpression);
				}

				if (e.NodeType == ExpressionType.Equal || e.NodeType == ExpressionType.NotEqual)
				{
					var binary = (BinaryExpression)e;
					if (binary.Left.IsNullValue() && binary.Right is SqlPlaceholderExpression placeholderRight)
					{
						return new TransformInfo(new SqlReaderIsNullExpression(placeholderRight, e.NodeType == ExpressionType.NotEqual), false, true);
					}
					if (binary.Right.IsNullValue() && binary.Left is SqlPlaceholderExpression placeholderLeft)
					{
						return new TransformInfo(new SqlReaderIsNullExpression(placeholderLeft, e.NodeType == ExpressionType.NotEqual), false, true);
					}
				}

				if (e is SqlReaderIsNullExpression isNullExpression)
				{
					if (isNullExpression.Placeholder.Index == null)
						throw new InvalidOperationException();

					Expression nullCheck = Expression.Call(
						DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						// ReSharper disable once CoVariantArrayConversion
						ExpressionInstances.Int32Array(isNullExpression.Placeholder.Index.Value));

					if (isNullExpression.IsNot)
						nullCheck = Expression.Not(nullCheck);

					return new TransformInfo(nullCheck);
				}

				if (e == RowCounterParam)
				{
					if (rowCounter == null)
					{
						rowCounter = e;
						expressionGenerator.AddVariable(RowCounterParam);
						expressionGenerator.AddExpression(Expression.Assign(RowCounterParam,
							Expression.Property(QueryRunnerParam, QueryRunner.RowsCountInfo)));
					}
				}

				return new TransformInfo(e);
			});

			return toRead;
		}
		/// <summary>
		/// 查询表达式转为实体的映射方法
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="expr"></param>
		/// <returns></returns>
		public Expression<Func<IQueryRunner,DBInstance,DbDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(SelectQueryClause query, Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			var expressionGenerator = new ExpressionGenerator();

			// variable accessed dynamically
			_ = expressionGenerator.AssignToVariable(DataReaderParam, "ldr");

			var readExpr = ToReadExpression(expressionGenerator, new NullabilityContext(query), expr);
			expressionGenerator.AddExpression(readExpr);

			var mappingBody = expressionGenerator.Build();

			var mapper = Expression.Lambda<Func<IQueryRunner, DBInstance, DbDataReader,Expression,object?[]?,object?[]?,T>>(mappingBody,
				QueryRunnerParam,
				ExpressionConstants.DataContextParam,
				DataReaderParam,
				ExpressionParam,
				ParametersParam,
				PreambleParam);

			return mapper;
		}

		#endregion

		#region BuildMultipleQuery

		public Expression? AssociationRoot;
		public Stack<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>? AssociationPath;

		#endregion

	}
}
