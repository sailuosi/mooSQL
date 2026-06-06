using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;



namespace mooSQL.linq
{
	using Linq;
	using Expressions;

	using PN = ProviderName;
    using mooSQL.linq.ext;

    public static partial class DbFunc
	{
		public enum AggregateModifier
		{
			None,
			Distinct,
			All,
		}

		public enum From
		{
			None,
			First,
			Last
		}

		public enum Nulls
		{
			None,
			Respect,
			Ignore
		}

		public enum NullsPosition
		{
			None,
			First,
			Last
		}
	}

	
	public static class AnalyticFunctions
	{
		/// <summary>
		/// Token name for analytic function. Used for resolving method chain.
		/// </summary>
		public const string FunctionToken  = "function";

		#region Call Builders

		sealed class OrderItemBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<DbFunc.NullsPosition>("nulls");
				switch (nulls)
				{
					case DbFunc.NullsPosition.None :
						break;
					case DbFunc.NullsPosition.First :
						builder.Expression += " NULLS FIRST";
						break;
					case DbFunc.NullsPosition.Last :
						builder.Expression += " NULLS LAST";
						break;
					default :
						throw new InvalidOperationException($"Unexpected nulls position: {nulls}");
				}
			}
		}

		sealed class ApplyAggregateModifier : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var modifier = builder.GetValue<DbFunc.AggregateModifier>("modifier");
				switch (modifier)
				{
					case DbFunc.AggregateModifier.None :
						break;
					case DbFunc.AggregateModifier.Distinct :
						builder.AddExpression("modifier", "DISTINCT");
						break;
					case DbFunc.AggregateModifier.All :
						builder.AddExpression("modifier", "ALL");
						break;
					default :
						throw new InvalidOperationException($"Unexpected aggregate modifier: {modifier}");
				}
			}
		}

		sealed class ApplyNullsModifier : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<DbFunc.Nulls>("nulls");
				var nullsStr = GetNullsStr(nulls);
				if (!string.IsNullOrEmpty(nullsStr))
					builder.AddExpression("modifier", nullsStr);
			}
		}

		static string GetNullsStr(DbFunc.Nulls nulls)
		{
			switch (nulls)
			{
				case DbFunc.Nulls.None   :
				case DbFunc.Nulls.Respect:
					// no need to add RESPECT NULLS, as it is default behavior and token itself supported only by Oracle, Informix and SQL Server 2022
					return string.Empty;
				case DbFunc.Nulls.Ignore :
					return "IGNORE NULLS";
				default :
					throw new InvalidOperationException($"Unexpected nulls: {nulls}");
			}
		}

		static string GetFromStr(DbFunc.From from)
		{
			switch (from)
			{
				case DbFunc.From.None :
					break;
				case DbFunc.From.First :
					return "FROM FIRST";
				case DbFunc.From.Last :
					return "FROM LAST";
				default :
					throw new InvalidOperationException($"Unexpected from: {from}");
			}
			return string.Empty;
		}

		sealed class ApplyFromAndNullsModifier : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<DbFunc.Nulls>("nulls");
				var from  = builder.GetValue<DbFunc.From>("from");

				var fromStr  = GetFromStr(from);
				var nullsStr = GetNullsStr(nulls);

				if (!string.IsNullOrEmpty(fromStr))
					builder.AddExpression("from", fromStr);
				if (!string.IsNullOrEmpty(nullsStr))
					builder.AddExpression("nulls", nullsStr);
			}
		}

		#endregion

		#region API Interfaces
		public interface IReadyToFunction<out TR>
		{
			[DbFunc.Extension("", ChainPrecedence = 0)]
			TR ToValue();
		}

		public interface IReadyToFunctionOrOverWithPartition<out TR> : IReadyToFunction<TR>
		{
			[DbFunc.Extension("OVER({query_partition_clause?})", TokenName = "over")]
			IOverMayHavePartition<TR> Over();
		}

		public interface IOverWithPartitionNeeded<out TR>
		{
			[DbFunc.Extension("OVER({query_partition_clause?})", TokenName = "over")]
			IOverMayHavePartition<TR> Over();
		}

		public interface INeedOrderByAndMaybeOverWithPartition<out TR>
		{
			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);
		}

		public interface INeedSingleOrderByAndMaybeOverWithPartition<out TR>
		{
			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);
		}

		public interface IOrderedAcceptOverReadyToFunction<out TR> : IReadyToFunctionOrOverWithPartition<TR>
		{
			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);

			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);
		}

		public interface IOverMayHavePartition<out TR> : IReadyToFunction<TR>
		{
			[DbFunc.Extension("PARTITION BY {partition_expr, ', '}", TokenName = "query_partition_clause")]
			IReadyToFunction<TR> PartitionBy([ExprParameter("partition_expr")] params object?[] expressions);
		}

		public interface IPartitionedMayHaveOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
		}

		public interface IOverMayHavePartitionAndOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
			[DbFunc.Extension("PARTITION BY {partition_expr, ', '}", TokenName = "query_partition_clause")]
			IPartitionedMayHaveOrder<TR> PartitionBy([ExprParameter("partition_expr")] params object?[] expressions);
		}

		public interface IAnalyticFunction<out TR>
		{
			[DbFunc.Extension("{function} OVER({query_partition_clause?}{_}{order_by_clause?}{_}{windowing_clause?})",
				TokenName = "over", ChainPrecedence = 10, IsWindowFunction = true)]
			IReadyForFullAnalyticClause<TR> Over();
		}

		public interface IAnalyticFunctionWithoutWindow<out TR>
		{
			[DbFunc.Extension("{function} OVER({query_partition_clause?}{_}{order_by_clause?})", TokenName = "over", ChainPrecedence = 10, IsWindowFunction = true)]
			IOverMayHavePartitionAndOrder<TR> Over();
		}

		public interface IAggregateFunction<out TR> : IAnalyticFunction<TR> {}
		public interface IAggregateFunctionSelfContained<out TR> : IAggregateFunction<TR>, IReadyToFunction<TR> {}

		public interface IOrderedReadyToFunction<out TR> : IReadyToFunction<TR>
		{
			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);

			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);
		}

		public interface INeedsWithinGroupWithOrderOnly<out TR>
		{
			[DbFunc.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "within_group")]
			INeedsOrderByOnly<TR> WithinGroup { get; }
		}

		public interface INeedsWithinGroupWithOrderAndMaybePartition<out TR>
		{
			[DbFunc.Extension("WITHIN GROUP ({order_by_clause}){_}{over?}", TokenName = "within_group")]
			INeedOrderByAndMaybeOverWithPartition<TR> WithinGroup { get; }
		}

		public interface INeedsWithinGroupWithSingleOrderAndMaybePartition<out TR>
		{
			[DbFunc.Extension("WITHIN GROUP ({order_by_clause}){_}{over?}", TokenName = "within_group")]
			INeedSingleOrderByAndMaybeOverWithPartition<TR> WithinGroup { get; }
		}

		public interface INeedsOrderByOnly<out TR>
		{
			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);
		}

		#region Full Support

		public interface IReadyForSortingWithWindow<out TR>
		{
			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);
		}

		public interface IReadyForFullAnalyticClause<out TR> : IReadyToFunction<TR>, IReadyForSortingWithWindow<TR>
		{
			[DbFunc.Extension("PARTITION BY {partition_expr, ', '}", TokenName = "query_partition_clause")]
			IPartitionDefinedReadyForSortingWithWindow<TR> PartitionBy([ExprParameter("partition_expr")] params object?[] expressions);
		}

		public interface IPartitionDefinedReadyForSortingWithWindow<out TR> : IReadyForSortingWithWindow<TR>, IReadyToFunction<TR>
		{
		}

		public interface IOrderedReadyToWindowing<out TR> : IReadyToFunction<TR>
		{
			[DbFunc.Extension("ROWS {boundary_clause}", TokenName = "windowing_clause")]
			IBoundaryExpected<TR> Rows { get; }

			[DbFunc.Extension("RANGE {boundary_clause}", TokenName = "windowing_clause")]
			IBoundaryExpected<TR> Range { get; }

			[DbFunc.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);

			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[DbFunc.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] DbFunc.NullsPosition nulls);
		}

		public interface IBoundaryExpected<out TR>
		{
			[DbFunc.Extension("UNBOUNDED PRECEDING", TokenName = "boundary_clause")]
			IReadyToFunction<TR> UnboundedPreceding { get; }

			[DbFunc.Extension("CURRENT ROW", TokenName = "boundary_clause")]
			IReadyToFunction<TR> CurrentRow { get; }

			[DbFunc.Extension("{value_expr} PRECEDING", TokenName = "boundary_clause")]
			IReadyToFunction<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);

			[DbFunc.Extension("BETWEEN {start_boundary} AND {end_boundary}", TokenName = "boundary_clause")]
			IBetweenStartExpected<TR> Between { get; }
		}

		public interface IBetweenStartExpected<out TR>
		{
			[DbFunc.Extension("UNBOUNDED PRECEDING", TokenName = "start_boundary")]
			IAndExpected<TR> UnboundedPreceding { get; }

			[DbFunc.Extension("CURRENT ROW", TokenName = "start_boundary")]
			IAndExpected<TR> CurrentRow { get; }

			[DbFunc.Extension("{value_expr} PRECEDING", TokenName = "start_boundary")]
			IAndExpected<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);
		}

		public interface IAndExpected<out TR>
		{
			// TokenName used only for chain continuation
			[DbFunc.Extension("", TokenName = "and_connector")]
			ISecondBoundaryExpected<TR> And { get; }
		}

		public interface ISecondBoundaryExpected<out TR>
		{
			[DbFunc.Extension("UNBOUNDED FOLLOWING", TokenName = "end_boundary")]
			IReadyToFunction<TR> UnboundedFollowing { get; }

			[DbFunc.Extension("CURRENT ROW", TokenName = "end_boundary")]
			IReadyToFunction<TR> CurrentRow { get; }

			[DbFunc.Extension("{value_expr} PRECEDING", TokenName = "end_boundary")]
			IReadyToFunction<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);

			[DbFunc.Extension("{value_expr} FOLLOWING", TokenName = "end_boundary")]
			IReadyToFunction<TR> ValueFollowing<T>([ExprParameter("value_expr")] T value);
		}

		#endregion Full Support

		#endregion API Interfaces

		#region Extensions

		[DbFunc.Extension("{function} FILTER (WHERE {filter})", TokenName = FunctionToken, ChainPrecedence = 2, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Filter<T>(this IAnalyticFunctionWithoutWindow<T> func,
			[ExprParameter] bool filter)
		{
			throw new LinqException($"'{nameof(Filter)}' is server-side method.");
		}

		#endregion

		#region Analytic functions

		#region Average

		[DbFunc.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static double Average<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Average)}' is server-side method.");
		}

		[DbFunc.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static double Average<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<double>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Average, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[DbFunc.Extension("AVG({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(Average)}' is server-side method.");
		}

		[DbFunc.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Average)}' is server-side method.");
		}

		#endregion Average

		#region Corr

		[DbFunc.Extension("CORR({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? Corr<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object?>> expr1, [ExprParameter] Expression<Func<T, object?>> expr2)
		{
			throw new LinqException($"'{nameof(Corr)}' is server-side method.");
		}

		[DbFunc.Extension("CORR({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? Corr<TEntity>(
			           this IQueryable<TEntity>               source,
			[ExprParameter] Expression<Func<TEntity, object?>> expr1,
			[ExprParameter] Expression<Func<TEntity, object?>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal?>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Corr, source, expr1, expr2),
					currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2)
				));
		}

		[DbFunc.Extension("CORR({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Corr<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(Corr)}' is server-side method.");
		}

		#endregion Corr

		#region Count

		[DbFunc.Extension("COUNT({expr})", IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, object?> expr)
		{
			throw new LinqException($"'{nameof(CountExt)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(CountExt)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CountExt, source, expr),
					currentSource.Expression, Expression.Quote(expr))
				);
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier = DbFunc.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CountExt, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[DbFunc.Extension("COUNT(*)", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true, CanBeNull = false)]
		public static IAggregateFunctionSelfContained<int> Count(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(Count)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true, CanBeNull = false)]
		public static IAggregateFunctionSelfContained<int> Count<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Count)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true, IsAggregate = true, CanBeNull = false)]
		public static IAggregateFunctionSelfContained<int> Count(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Count)}' is server-side method.");
		}

		#endregion

		#region LongCount

		[DbFunc.Extension("COUNT({expr})", IsAggregate = true, ChainPrecedence = 0)]
		public static long LongCountExt<TEntity>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, object?> expr)
		{
			throw new LinqException($"'{nameof(LongCountExt)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static long LongCountExt<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(LongCountExt)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static long LongCountExt<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier = DbFunc.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<long>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(LongCountExt, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[DbFunc.Extension("COUNT(*)", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<long> LongCount(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(LongCount)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<long> LongCount<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(LongCount)}' is server-side method.");
		}

		[DbFunc.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<long> LongCount(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(LongCount)}' is server-side method.");
		}

		#endregion

		#region CovarPop

		[DbFunc.Extension("COVAR_POP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal CovarPop<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object?>> expr1, [ExprParameter] Expression<Func<T, object?>> expr2)
		{
			throw new LinqException($"'{nameof(CovarPop)}' is server-side method.");
		}

		[DbFunc.Extension("COVAR_POP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal CovarPop<TEntity>(
			           this IQueryable<TEntity>               source,
			[ExprParameter] Expression<Func<TEntity, object?>> expr1,
			[ExprParameter] Expression<Func<TEntity, object?>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CovarPop, source, expr1, expr2),
					currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2)
				));
		}

		[DbFunc.Extension("COVAR_POP({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> CovarPop<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new LinqException($"'{nameof(CovarPop)}' is server-side method.");
		}

		#endregion CovarPop

		#region CovarSamp

		[DbFunc.Extension("COVAR_SAMP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? CovarSamp<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object?>> expr1, [ExprParameter] Expression<Func<T, object?>> expr2)
		{
			throw new LinqException($"'{nameof(CovarSamp)}' is server-side method.");
		}

		[DbFunc.Extension("COVAR_SAMP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? CovarSamp<TEntity>(
			           this IQueryable<TEntity>                source,
			[ExprParameter] Expression<Func<TEntity, object?>> expr1,
			[ExprParameter] Expression<Func<TEntity, object?>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CovarSamp, source, expr1, expr2),
					currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2)
				));
		}

		[DbFunc.Extension("COVAR_SAMP({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> CovarSamp<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new LinqException($"'{nameof(CovarSamp)}' is server-side method.");
		}

		#endregion CovarSamp

		[DbFunc.Extension("CUME_DIST({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<TR> CumeDist<TR>(this DbFunc.ISqlExtension? ext, [ExprParameter] params object?[] expr)
		{
			throw new LinqException($"'{nameof(CumeDist)}' is server-side method.");
		}

		[DbFunc.Extension("CUME_DIST()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<TR> CumeDist<TR>(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(CumeDist)}' is server-side method.");
		}

		[DbFunc.Extension("DENSE_RANK({expr1}, {expr2}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<long> DenseRank(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(DenseRank)}' is server-side method.");
		}

		[DbFunc.Extension("DENSE_RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<long> DenseRank(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(DenseRank)}' is server-side method.");
		}

		[DbFunc.Extension("FIRST_VALUE({expr}){_}{modifier?}", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true, Configuration = ProviderName.SqlServer2022)]
		[DbFunc.Extension("FIRST_VALUE({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> FirstValue<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.Nulls nulls)
		{
			throw new LinqException($"'{nameof(FirstValue)}' is server-side method.");
		}

		[DbFunc.Extension("LAG({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.Nulls nulls)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[DbFunc.Extension("LAG({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[DbFunc.Extension("LAG({expr}, {offset})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[DbFunc.Extension("LAG({expr}, {offset}, {default})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[DbFunc.Extension("LAG({expr}{_}{modifier?}, {offset}, {default})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.Nulls nulls, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[DbFunc.Extension("LAST_VALUE({expr}){_}{modifier?}", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true, Configuration = ProviderName.SqlServer2022)]
		[DbFunc.Extension("LAST_VALUE({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> LastValue<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.Nulls nulls)
		{
			throw new LinqException($"'{nameof(LastValue)}' is server-side method.");
		}

		[DbFunc.Extension("LEAD({expr}{_}{modifier?})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.Nulls nulls)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[DbFunc.Extension("LEAD({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[DbFunc.Extension("LEAD({expr}, {offset})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[DbFunc.Extension("LEAD({expr}, {offset}, {default})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[DbFunc.Extension("LEAD({expr}{_}{modifier?}, {offset}, {default})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.Nulls nulls, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[DbFunc.Extension("LISTAGG({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderAndMaybePartition<string> ListAgg<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(ListAgg)}' is server-side method.");
		}

		[DbFunc.Extension("LISTAGG({expr}, {delimiter}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderAndMaybePartition<string> ListAgg<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] string delimiter)
		{
			throw new LinqException($"'{nameof(ListAgg)}' is server-side method.");
		}

		#region Max

		[DbFunc.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Max<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Max)}' is server-side method.");
		}

		[DbFunc.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Max<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Max, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[DbFunc.Extension("MAX({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Max)}' is server-side method.");
		}

		[DbFunc.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Max)}' is server-side method.");
		}

		#endregion Max

		#region Median

		[DbFunc.Extension("MEDIAN({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static long Median<TEntity, T>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, T> expr)
		{
			throw new LinqException($"'{nameof(Median)}' is server-side method.");
		}

		[DbFunc.Extension("MEDIAN({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static long Median<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<long>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Median, source, expr),
					currentSource.Expression, Expression.Quote(expr)
				));
		}

		[DbFunc.Extension("MEDIAN({expr}) {over}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IReadyToFunctionOrOverWithPartition<T> Median<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Median)}' is server-side method.");
		}

		#endregion Median

		#region Min

		[DbFunc.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Min<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Min)}' is server-side method.");
		}

		[DbFunc.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Min<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Min, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));
		}

		[DbFunc.Extension("MIN({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Min)}' is server-side method.");
		}

		[DbFunc.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Min)}' is server-side method.");
		}

		#endregion Min

		[DbFunc.Extension("NTH_VALUE({expr}, {n})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] long n)
		{
			throw new LinqException($"'{nameof(NthValue)}' is server-side method.");
		}

		[DbFunc.Extension("NTH_VALUE({expr}, {n}){_}{from?}{_}{nulls?}", TokenName = FunctionToken, BuilderType = typeof(ApplyFromAndNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] long n, [SqlQueryDependent] DbFunc.From from, [SqlQueryDependent] DbFunc.Nulls nulls)
		{
			throw new LinqException($"'{nameof(NthValue)}' is server-side method.");
		}

		[DbFunc.Extension("NTILE({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> NTile<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(NTile)}' is server-side method.");
		}

		[DbFunc.Extension("PERCENT_RANK({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<T> PercentRank<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] params object?[] expr)
		{
			throw new LinqException($"'{nameof(PercentRank)}' is server-side method.");
		}

		[DbFunc.Extension("PERCENT_RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> PercentRank<T>(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(PercentRank)}' is server-side method.");
		}

		[DbFunc.Extension("PERCENTILE_CONT({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithSingleOrderAndMaybePartition<T> PercentileCont<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(PercentileCont)}' is server-side method.");
		}

		//TODO: check nulls support when ordering
		[DbFunc.Extension("PERCENTILE_DISC({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithSingleOrderAndMaybePartition<T> PercentileDisc<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(PercentileDisc)}' is server-side method.");
		}

		[DbFunc.Extension("RANK({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<long> Rank(this DbFunc.ISqlExtension? ext, [ExprParameter] params object?[] expr)
		{
			throw new LinqException($"'{nameof(Rank)}' is server-side method.");
		}

		[DbFunc.Extension("RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<long> Rank(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(Rank)}' is server-side method.");
		}

		[DbFunc.Extension("RATIO_TO_REPORT({expr}) {over}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IOverWithPartitionNeeded<TR> RatioToReport<TR>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(RatioToReport)}' is server-side method.");
		}

		#region REGR_ function

		[DbFunc.Extension("REGR_SLOPE({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSlope<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSlope)}' is server-side method.");
		}

		[DbFunc.Extension("REGR_INTERCEPT({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrIntercept<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrIntercept)}' is server-side method.");
		}

		[DbFunc.Extension("REGR_COUNT({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<long> RegrCount(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrCount)}' is server-side method.");
		}

		[DbFunc.Extension("REGR_R2({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrR2<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrR2)}' is server-side method.");
		}

		[DbFunc.Extension("REGR_AVGX({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrAvgX<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrAvgX)}' is server-side method.");
		}

		[DbFunc.Extension("REGR_AVGY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrAvgY<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrAvgY)}' is server-side method.");
		}

		// ReSharper disable once InconsistentNaming
		[DbFunc.Extension("REGR_SXX({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSXX<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSXX)}' is server-side method.");
		}

		// ReSharper disable once InconsistentNaming
		[DbFunc.Extension("REGR_SYY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSYY<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSYY)}' is server-side method.");
		}

		// ReSharper disable once InconsistentNaming
		[DbFunc.Extension("REGR_SXY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSXY<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSXY)}' is server-side method.");
		}

		#endregion

		[DbFunc.Extension("ROW_NUMBER()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true, CanBeNull = false)]
		public static IAnalyticFunctionWithoutWindow<long> RowNumber(this DbFunc.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(RowNumber)}' is server-side method.");
		}

		#region StdDev

		[DbFunc.Extension(              "STDEV({expr})",  TokenName = FunctionToken, ChainPrecedence = 0, IsWindowFunction = true)]
		[DbFunc.Extension(PN.Oracle,    "STDDEV({expr})", TokenName = FunctionToken, ChainPrecedence = 0, IsWindowFunction = true)]
		public static double? StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		[DbFunc.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		[DbFunc.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		public static double? StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		[DbFunc.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		[DbFunc.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		public static double? StdDev<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier = DbFunc.AggregateModifier.None )
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<double>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StdDev, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));
		}

		[DbFunc.Extension(              "STDEV({expr})",  TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		[DbFunc.Extension(PN.Oracle,    "STDDEV({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		[DbFunc.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		[DbFunc.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		#endregion StdDev

		#region StdDevPop

		[DbFunc.Extension("STDDEV_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal StdDevPop<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(StdDevPop)}' is server-side method.");
		}

		[DbFunc.Extension("STDDEV_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal StdDevPop<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StdDevPop, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[DbFunc.Extension("STDDEV_POP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDevPop<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(StdDevPop)}' is server-side method.");
		}

		#endregion StdDevPop

		#region StdDevSamp

		[DbFunc.Extension("STDDEV_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? StdDevSamp<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(StdDevSamp)}' is server-side method.");
		}

		[DbFunc.Extension("STDDEV_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? StdDevSamp<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StdDevSamp, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[DbFunc.Extension("STDDEV_SAMP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDevSamp<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(StdDevSamp)}' is server-side method.");
		}

		#endregion StdDevSamp

		[DbFunc.Extension("SUM({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Sum)}' is server-side method.");
		}

		[DbFunc.Extension("SUM({modifier?}{_}{expr})" , BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Sum)}' is server-side method.");
		}

		#region VarPop

		[DbFunc.Extension("VAR_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal VarPop<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(VarPop)}' is server-side method.");
		}

		[DbFunc.Extension("VAR_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal VarPop<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(VarPop, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[DbFunc.Extension("VAR_POP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> VarPop<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(VarPop)}' is server-side method.");
		}

		#endregion VarPop

		#region VarSamp

		[DbFunc.Extension("VAR_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? VarSamp<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(VarSamp)}' is server-side method.");
		}

		[DbFunc.Extension("VAR_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? VarSamp<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(VarSamp, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[DbFunc.Extension("VAR_SAMP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> VarSamp<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(VarSamp)}' is server-side method.");
		}

		#endregion VarSamp

		#region Variance

		[DbFunc.Extension("VARIANCE({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Variance<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		[DbFunc.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Variance<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		[DbFunc.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Variance<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier = DbFunc.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Variance, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));
		}

		[DbFunc.Extension("VARIANCE({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		[DbFunc.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this DbFunc.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] DbFunc.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		#endregion

		[DbFunc.Extension("{function} KEEP (DENSE_RANK FIRST {order_by_clause}){_}{over?}", ChainPrecedence = 10, IsWindowFunction = true)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepFirst<TR>(this IAggregateFunction<TR> ext)
		{
			throw new LinqException($"'{nameof(KeepFirst)}' is server-side method.");
		}

		[DbFunc.Extension("{function} KEEP (DENSE_RANK LAST {order_by_clause}){_}{over?}", ChainPrecedence = 10, IsWindowFunction = true)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepLast<TR>(this IAggregateFunction<TR> ext)
		{
			throw new LinqException($"'{nameof(KeepLast)}' is server-side method.");
		}

		#endregion Analytic functions
	}

}
