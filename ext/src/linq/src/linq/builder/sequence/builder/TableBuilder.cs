using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
    using mooSQL.utils;
    using mooSQL.data;

    [BuildsMethodCall("AsCte", "GetCte", "FromSql", "FromSqlScalar", CanBuildName = nameof(CanBuildKnownMethods))]
	[BuildsMethodCall("GetTable", "TableFromExpression", CanBuildName = nameof(CanBuildTableMethods))]
	[BuildsExpression(ExpressionType.Call, CanBuildName = nameof(CanBuildAttributedMethods))]
	sealed partial class TableBuilder : ISequenceBuilder
	{
		public static bool CanBuildKnownMethods(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> true;

		public static bool CanBuildTableMethods(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> typeof(ITable<>).IsSameOrParentOf(call.Type);

		public static bool CanBuildAttributedMethods(Expression expr, BuildInfo info, ExpressionBuilder builder)
			=> ((MethodCallExpression)expr).Method.GetTableFunctionAttribute(builder.DBLive) != null;

		enum BuildContextType
		{
			None,
			GetTableMethod,
			TableFunctionAttribute,
			TableFromExpression,
			AsCteMethod,
			GetCteMethod,
			FromSqlMethod,
			FromSqlScalarMethod
		}

		static BuildContextType FindBuildContext(ExpressionBuilder builder, BuildInfo buildInfo, out IBuildContext? parentContext)
		{
			parentContext = null;

			var expression = buildInfo.Expression;

			switch (expression.NodeType)
			{
				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)expression;

					switch (mc.Method.Name)
					{
						case "GetTable" 
							when typeof(ITable<>).IsSameOrParentOf(expression.Type):
						{
							return BuildContextType.GetTableMethod;
						}

						case "TableFromExpression"
							when typeof(ITable<>).IsSameOrParentOf(expression.Type):
						{
							return BuildContextType.TableFromExpression;
						}

						case "AsCte":
							return BuildContextType.AsCteMethod;

						case "GetCte":
							return BuildContextType.GetCteMethod;

						case "FromSql":
							return BuildContextType.FromSqlMethod;

						case "FromSqlScalar":
							return BuildContextType.FromSqlScalarMethod;
					}

					var attr = mc.Method.GetTableFunctionAttribute(builder.DBLive);

					if (attr != null)
						return BuildContextType.TableFunctionAttribute;

					break;
				}
			}

			return BuildContextType.None;
		}
		/// <summary>
		/// 暂停该逻辑，未知用途
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="DB"></param>
		/// <param name="entityType"></param>
		/// <param name="tableExpression"></param>
		/// <returns></returns>
		static Expression ApplyQueryFilters(ExpressionBuilder builder, DBInstance DB, Type entityType, Expression tableExpression)
		{
			//if (builder.IsFilterDisabled(entityType))
				return tableExpression;

			//var testEd = DB.client.EntityCash.getEntityInfo(entityType);

			//if (testEd.QueryFilterLambda == null && testEd.QueryFilterFunc == null)
			//	return tableExpression;

			//Expression filteredExpression;

			//if (testEd.QueryFilterFunc == null)
			//{
			//	// shortcut for simple case. We know that MappingSchema is read only and can be sure that comparing cache will not require complex logic.

			//	var dcParam = testEd.QueryFilterLambda!.Parameters[1];
			//	var dcExpr  = SqlQueryRootExpression.Create(mappingSchema, dcParam.Type);

			//	var filterLambda = Expression.Lambda(testEd.QueryFilterLambda.Body.Replace(dcParam, dcExpr), testEd.QueryFilterLambda.Parameters[0]);

			//	// to avoid recursion
			//	filteredExpression = Expression.Call(Methods.LinqToDB.IgnoreFilters.MakeGenericMethod(entityType), tableExpression, ExpressionInstances.EmptyTypes);

			//	filteredExpression = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), filteredExpression, Expression.Quote(filterLambda));
			//	filteredExpression = ExpressionBuilder.ExposeExpression(filteredExpression, builder.DataContext, builder.OptimizationContext, builder.ParameterValues, optimizeConditions: true, compactBinary: true);
			//}
			//else
			//{
			//	// Closure should capture mappingSchema, entityType and tableExpression only. Used in EqualsToVisitor
			//	filteredExpression = builder.ParametersContext.RegisterDynamicExpressionAccessor(tableExpression, builder.DataContext, DB, (dc, ms) =>
			//	{
			//		var ed = ms.GetEntityDescriptor(entityType, dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

			//		var filterLambdaExpr = ed.QueryFilterLambda;
			//		var filterFunc       = ed.QueryFilterFunc;

			//		// to avoid recursion
			//		Expression sequenceExpr = Expression.Call(Methods.LinqToDB.IgnoreFilters.MakeGenericMethod(entityType), tableExpression, ExpressionInstances.EmptyTypes);

			//		if (filterLambdaExpr != null)
			//		{
			//			var dcParam = filterLambdaExpr.Parameters[1];
			//			var dcExpr  = SqlQueryRootExpression.Create(ms, dcParam.Type);

			//			var filterLambda = Expression.Lambda(filterLambdaExpr.Body.Replace(dcParam, dcExpr), filterLambdaExpr.Parameters[0]);

			//			sequenceExpr = Expression.Call(Methods.Queryable.Where.MakeGenericMethod(entityType), sequenceExpr, Expression.Quote(filterLambda));
			//		}

			//		if (filterFunc != null)
			//		{
			//			var query    = ExpressionQueryImpl.CreateQuery(entityType, dc, sequenceExpr);
			//			var filtered = (IQueryable)filterFunc.DynamicInvoke(query, dc)!;

			//			sequenceExpr = filtered.Expression;
			//		}


			//		// Optimize conditions and compact binary expressions
			//		var optimizationContext = new ExpressionTreeOptimizationContext(dc);
			//		sequenceExpr = ExpressionBuilder.ExposeExpression(sequenceExpr, dc, optimizationContext, null, optimizeConditions : true, compactBinary : true);

			//		return sequenceExpr;
			//	});
			//}

			//return filteredExpression;
		}



		BuildSequenceResult BuildTableWithAppliedFilters(ExpressionBuilder builder, BuildInfo buildInfo, DBInstance mappingSchema, Expression tableExpression)
		{
			var entityType      = tableExpression.Type.GetGenericArguments()[0];
			var applied         = ApplyQueryFilters(builder, builder.DBLive, entityType, tableExpression);

			if (!ReferenceEquals(applied, tableExpression))
			{
				return builder.TryBuildSequence(new BuildInfo(buildInfo, applied));
			}

			var tableContext = new TableContext(builder, buildInfo,entityType);
			builder.TablesInScope?.Add(tableContext);
			return BuildSequenceResult.FromContext(tableContext);
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var type = FindBuildContext(builder, buildInfo, out var parentContext);

			switch (type)
			{
				case BuildContextType.None                   : return BuildSequenceResult.NotSupported();

				case BuildContextType.GetTableMethod         :
				{
					var mc = (MethodCallExpression)buildInfo.Expression;


					return BuildTableWithAppliedFilters(builder, buildInfo, builder.DBLive, buildInfo.Expression);
				}

				case BuildContextType.TableFunctionAttribute :
				{
					

					return BuildSequenceResult.FromContext(new TableContext(builder, builder.DBLive, buildInfo));
				}

				case BuildContextType.TableFromExpression    :
				{
					

					var mc = (MethodCallExpression)buildInfo.Expression;

					var bodyMethod = mc.Arguments[1].UnwrapLambda().Body;

					return BuildSequenceResult.FromContext(new TableContext(builder, builder.DBLive, new BuildInfo(buildInfo, bodyMethod)));
				}
				case BuildContextType.AsCteMethod            : return BuildCteContext     (builder, buildInfo);
				case BuildContextType.GetCteMethod           : return BuildRecursiveCteContextTable (builder, buildInfo);
				case BuildContextType.FromSqlMethod          : return BuildRawSqlTable(builder, buildInfo, false);
				case BuildContextType.FromSqlScalarMethod    : return BuildRawSqlTable(builder, buildInfo, true);
			}

			throw new InvalidOperationException();
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

	}
}
