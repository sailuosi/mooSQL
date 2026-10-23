using System;
using System.Linq;
using System.Linq.Expressions;

using PN = mooSQL.linq.ProviderName;

namespace mooSQL.linq
{
	public static partial class SooFunctionExtension
	{
		/// <summary>
		/// OrderBy&lt;T, TR, TKey&gt;
		/// </summary>
		[SooFunctionExtension.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[SooFunctionExtension.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[SooFunctionExtension.Extension("{expr}",                           TokenName = "order_item")]
		public static SooFunctionExtension.IAggregateFunctionOrdered<T, TR> OrderBy<T, TR, TKey>(
							this SooFunctionExtension.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new SooFunctionExtension.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}
		/// <summary>
		/// OrderBy&lt;T, TR&gt;
		/// </summary>

		[SooFunctionExtension.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[SooFunctionExtension.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[SooFunctionExtension.Extension("{aggregate}",                      TokenName = "order_item")]
		public static SooFunctionExtension.IAggregateFunction<T, TR> OrderBy<T, TR>(
			this SooFunctionExtension.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate), aggregate.Query.Expression
				));

			return new SooFunctionExtension.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}
		/// <summary>
		/// OrderByDescending&lt;T, TR, TKey&gt;
		/// </summary>

		[SooFunctionExtension.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[SooFunctionExtension.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[SooFunctionExtension.Extension("{expr} DESC",                      TokenName = "order_item")]
		public static SooFunctionExtension.IAggregateFunctionOrdered<T, TR> OrderByDescending<T, TR, TKey>(
							this SooFunctionExtension.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new SooFunctionExtension.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}
		/// <summary>
		/// OrderByDescending&lt;T, TR&gt;
		/// </summary>

		[SooFunctionExtension.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[SooFunctionExtension.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[SooFunctionExtension.Extension("{aggregate} DESC",                 TokenName = "order_item")]
		public static SooFunctionExtension.IAggregateFunction<T, TR> OrderByDescending<T, TR>(
			this SooFunctionExtension.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate),
					Expression.Constant(aggregate)
				));

			return new SooFunctionExtension.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}
		/// <summary>
		/// ThenBy&lt;T, TR, TKey&gt;
		/// </summary>

		[SooFunctionExtension.Extension("{expr}", TokenName = "order_item")]
		public static SooFunctionExtension.IAggregateFunctionOrdered<T, TR> ThenBy<T, TR, TKey>(
							this SooFunctionExtension.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>            expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new SooFunctionExtension.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}
		/// <summary>
		/// ThenByDescending&lt;T, TR, TKey&gt;
		/// </summary>

		[SooFunctionExtension.Extension("{expr} DESC", TokenName = "order_item")]
		public static SooFunctionExtension.IAggregateFunctionOrdered<T, TR> ThenByDescending<T, TR, TKey>(
							this SooFunctionExtension.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>        expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new SooFunctionExtension.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		// For Oracle we always define at least one ordering by rownum. If ordering defined explicitly, this definition will be replaced.
		/// <summary>
		/// ToValue&lt;T, TR&gt;
		/// </summary>
		[SooFunctionExtension.Extension(PN.Oracle,       "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[SooFunctionExtension.Extension(PN.OracleNative, "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[SooFunctionExtension.Extension(                  "",                                                                  ChainPrecedence = 0, IsAggregate = true)]
		public static TR ToValue<T, TR>(this SooFunctionExtension.IAggregateFunction<T, TR> aggregate)
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

	public static partial class SooFunctionExtension
	{
		/// <summary>
		/// IAggregateFunction&lt;T, TR&gt;
		/// </summary>
		/// <summary>
		/// IAggregateFunction&lt;T, TR&gt;
		/// </summary>
		public interface IAggregateFunction<out T, out TR> : DbFunc.IQueryableContainer
		{

		}
		/// <summary>
		/// IAggregateFunctionNotOrdered&lt;T, TR&gt;
		/// </summary>

		public interface IAggregateFunctionNotOrdered<out T, out TR> : IAggregateFunction<T, TR>
		{
		}
		/// <summary>
		/// IAggregateFunctionOrdered&lt;T, TR&gt;
		/// </summary>

		public interface IAggregateFunctionOrdered<out T, out TR> : IAggregateFunction<T, TR>
		{
		}
		/// <summary>
		/// AggregateFunctionNotOrderedImpl&lt;T, TR&gt;
		/// </summary>

		public class AggregateFunctionNotOrderedImpl<T, TR> : IAggregateFunctionNotOrdered<T, TR>, IAggregateFunctionOrdered<T, TR>
		{
			/// <summary>
			/// AggregateFunctionNotOrderedImpl
			/// </summary>
			/// <summary>
			/// AggregateFunctionNotOrderedImpl
			/// </summary>
			public AggregateFunctionNotOrderedImpl(IQueryable<TR> query)
			{
				Query = query ?? throw new ArgumentNullException(nameof(query));
			}
			/// <summary>
			/// Gets or sets Query.
			/// </summary>

			public IQueryable Query { get; }
		}
	}
}
