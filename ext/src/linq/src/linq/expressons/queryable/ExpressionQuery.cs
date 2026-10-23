using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq
{
	using mooSQL.linq.utils;
    using mooSQL.data;
    using mooSQL.linq.translator;
    /// <summary>
    /// ExpressionQuery type.
    /// </summary>

    public abstract class ExpressionQuery<T> : IExpressionQuery<T>
#if NET5_0_OR_GREATER
        , IAsyncEnumerable<T>
#endif
	{
		#region Init
		/// <summary>
		/// Init
		/// </summary>

		protected void Init(DBInstance dataContext, Expression? expression)
		{
			DBLive = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			Expression  = expression  ?? Expression.Constant(this);
			//if (dataContext.MappingSchema.client == null) {
			//	dataContext.MappingSchema.client = dataContext.DB.client;

            //}
		}
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>

		public Expression   Expression  { get; set; } = null!;
		/// <summary>
		/// Gets or sets DBLive.
		/// </summary>

		public DBInstance DBLive { get; set; }
		internal SentenceBag<T>? Info;
		internal object?[]? Parameters;

		RunnerContext MakeContext(SentenceBag query, Expression expression, CancellationToken cancellationToken = default)
			=> RunnerContextFactory.Create(query, DBLive, expression, Parameters, cancellationToken);

		#endregion

		#region Public Members

#if DEBUG
		// This property is helpful in Debug Mode.
		//

		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// _sqlText
		/// </summary>
		/// <summary>
		/// _sqlText
		/// </summary>
		public string _sqlText => SqlText;
#endif
		/// <summary>
		/// SqlText
		/// </summary>

		public string SqlText
		{
			get
			{
				var expression = Expression;
				var info       = GetQuery(ref expression, true, out var dependsOnParameters);

				if (!dependsOnParameters)
					Expression = expression;

				var sqlText    = SentenceExecutor.GetSqlText(info, DBLive, expression, Parameters);

				return sqlText;
			}
		}

		#endregion

		#region Execute

		SentenceBag<T> GetQuery(ref Expression expression, bool cache, out bool dependsOnParameters)
		{
			dependsOnParameters = false;

			if (cache && Info != null)
				return Info;

			var info = QueryMate.GetQuery<T>(DBLive, ref expression, out dependsOnParameters);

			if (cache && !dependsOnParameters && info.IsCacheable)
				Info = info;

			return info;
		}
		/// <summary>
		/// ExecuteAsync&lt;TResult&gt;
		/// </summary>

		public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var query = GetQuery(ref expression, false, out _);

			var value = await query.Runner.loadElementAsync(MakeContext(query, expression, cancellationToken))
				.ConfigureAwait(mooSQL.linq.ExtLinqOptions.ContinueOnCapturedContext);

			return (TResult)value!;
		}

#if NET6_0_OR_GREATER
        /// <summary>
        /// ExecuteAsyncEnumerable&lt;TResult&gt;
        /// </summary>
        /// <summary>
        /// ExecuteAsyncEnumerable&lt;TResult&gt;
        /// </summary>
        public async Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var query = GetQuery(ref expression, false, out _);

			return QueryMate.GetQuery<TResult>(DBLive, ref expression, out _).Runner.loadResultList(MakeContext(query, expression, cancellationToken));
		}
#endif

		IDisposable? StartLoadTransaction(SentenceBag query)
		{
			// Do not start implicit transaction if there is no preambles
			return null;
		}
        /// <summary>
        /// Gets GetForEachAsync.
        /// </summary>

        public async Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			var transaction = StartLoadTransaction(query);

			var context = MakeContext(query, expression, cancellationToken);

#if NET6_0_OR_GREATER
            var enumerable = (IAsyncEnumerable<T>)query.Runner.loadResultList(context);
#pragma warning disable CA2007
            await using var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
#pragma warning restore CA2007

			while (await enumerator.MoveNextAsync().ConfigureAwait(mooSQL.linq.ExtLinqOptions.ContinueOnCapturedContext))
			{
				action(enumerator.Current);
			}
#else
            var enumerable = query.Runner.loadResultList(context);
            using var enumerator = enumerable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                action(enumerator.Current);
            }
#endif
        }
        /// <summary>
        /// Gets GetForEachUntilAsync.
        /// </summary>

        public async Task GetForEachUntilAsync(Func<T,bool> func, CancellationToken cancellationToken)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			var context = MakeContext(query, expression, cancellationToken);

#if NET6_0_OR_GREATER
            var enumerable = (IAsyncEnumerable<T>)query.Runner.loadResultList(context);
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);

			while (await enumerator.MoveNextAsync().ConfigureAwait(mooSQL.linq.ExtLinqOptions.ContinueOnCapturedContext))
			{
				if (func(enumerator.Current))
					break;
			}
#else
            var enumerable = query.Runner.loadResultList(context);
            var enumerator = enumerable.GetEnumerator();

            while ( enumerator.MoveNext())
            {
                if (func(enumerator.Current))
                    break;
            }
#endif

        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Gets GetAsyncEnumerable.
        /// </summary>
        /// <summary>
        /// Gets GetAsyncEnumerable.
        /// </summary>
        public IAsyncEnumerable<T> GetAsyncEnumerable()
		{
			return this;
		}
		/// <summary>
		/// Gets GetAsyncEnumerator.
		/// </summary>

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			using (StartLoadTransaction(query))
			{
				return query.Runner.loadResultList(MakeContext(query, expression, cancellationToken))
					.GetAsyncEnumerator(cancellationToken);
			}
		}
#endif

		#endregion

		#region IQueryable Members

		Type           IQueryable.ElementType => typeof(T);
		Expression     IQueryable.Expression  => Expression;
		IQueryProvider IQueryable.Provider    => this;

		#endregion

		#region IQueryProvider Members

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			return new ExpressionQueryImpl<TElement>(DBLive, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator.CreateInstance(
					typeof(ExpressionQueryImpl<>).MakeGenericType(elementType),
					DBLive, expression)!;
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException ?? ex;
			}
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecuteT);

			var query = GetQuery(ref expression, false, out _);

			using (StartLoadTransaction(query))
			{
                var res = query.Runner.loadElement(MakeContext(query, expression));
				if (res == null) {
					return default(TResult);
				}
                return (TResult)res;
			}
		}

		object? IQueryProvider.Execute(Expression expression)
		{
			using var m = ActivityService.Start(ActivityID.QueryProviderExecute);

			var query = GetQuery(ref expression, false, out _);

			using (StartLoadTransaction(query))
			{
				return query.Runner.loadElement(MakeContext(query, expression));
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumeratorT);

			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			using (StartLoadTransaction(query))
			{
				return query.Runner.loadResultList(MakeContext(query, expression)).GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			using var _ = ActivityService.Start(ActivityID.QueryProviderGetEnumerator);

			var expression = Expression;
			var query      = GetQuery(ref expression, true, out var dependsOnParameters);

			if (!dependsOnParameters)
				Expression = expression;

			using (StartLoadTransaction(query))
			{
                return query.Runner.loadResultList(MakeContext(query, expression)).GetEnumerator();
			}
		}

		#endregion


	}
}
