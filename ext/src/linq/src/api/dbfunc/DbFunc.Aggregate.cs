using System;
using System.Linq;
using System.Linq.Expressions;

using PN = mooSQL.linq.ProviderName;

namespace mooSQL.linq
{
	using Linq;

	public static class StringAggregateExtensions
	{
		[DbFunc.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[DbFunc.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[DbFunc.Extension("{expr}",                           TokenName = "order_item")]
		public static DbFunc.IAggregateFunctionOrdered<T, TR> OrderBy<T, TR, TKey>(
							this DbFunc.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new DbFunc.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[DbFunc.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[DbFunc.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[DbFunc.Extension("{aggregate}",                      TokenName = "order_item")]
		public static DbFunc.IAggregateFunction<T, TR> OrderBy<T, TR>(
			this DbFunc.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate), aggregate.Query.Expression
				));

			return new DbFunc.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[DbFunc.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[DbFunc.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[DbFunc.Extension("{expr} DESC",                      TokenName = "order_item")]
		public static DbFunc.IAggregateFunctionOrdered<T, TR> OrderByDescending<T, TR, TKey>(
							this DbFunc.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new DbFunc.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[DbFunc.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[DbFunc.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[DbFunc.Extension("{aggregate} DESC",                 TokenName = "order_item")]
		public static DbFunc.IAggregateFunction<T, TR> OrderByDescending<T, TR>(
			this DbFunc.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate),
					Expression.Constant(aggregate)
				));

			return new DbFunc.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[DbFunc.Extension("{expr}", TokenName = "order_item")]
		public static DbFunc.IAggregateFunctionOrdered<T, TR> ThenBy<T, TR, TKey>(
							this DbFunc.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>            expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new DbFunc.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[DbFunc.Extension("{expr} DESC", TokenName = "order_item")]
		public static DbFunc.IAggregateFunctionOrdered<T, TR> ThenByDescending<T, TR, TKey>(
							this DbFunc.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>        expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new DbFunc.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		// For Oracle we always define at least one ordering by rownum. If ordering defined explicitly, this definition will be replaced.
		[DbFunc.Extension(PN.Oracle,       "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[DbFunc.Extension(PN.OracleNative, "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[DbFunc.Extension(                  "",                                                                  ChainPrecedence = 0, IsAggregate = true)]
		public static TR ToValue<T, TR>(this DbFunc.IAggregateFunction<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			Expression aggregateExpr;

			if (aggregate is DbFunc.IQueryableContainer)
			{
				aggregateExpr = aggregate.Query.Expression;
			}
			else
			{
				aggregateExpr = Expression.Constant(aggregate);
			}

			return aggregate.Query.Provider.Execute<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ToValue, aggregate),
					aggregateExpr));
		}
	}

	public static partial class DbFunc
	{
		public interface IAggregateFunction<out T, out TR> : IQueryableContainer
		{

		}

		public interface IAggregateFunctionNotOrdered<out T, out TR> : IAggregateFunction<T, TR>
		{
		}

		public interface IAggregateFunctionOrdered<out T, out TR> : IAggregateFunction<T, TR>
		{
		}

		public class AggregateFunctionNotOrderedImpl<T, TR> : IAggregateFunctionNotOrdered<T, TR>, IAggregateFunctionOrdered<T, TR>
		{
			public AggregateFunctionNotOrderedImpl(IQueryable<TR> query)
			{
				Query = query ?? throw new ArgumentNullException(nameof(query));
			}

			public IQueryable Query { get; }
		}
	}
}
