
using mooSQL.data;
using mooSQL.linq.Async;
using mooSQL.linq.Data;
using mooSQL.linq.Extensions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;
using mooSQL.linq.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class EntityProvider: IQueryProviderAsync
    {

        public ProviderContext providerContext;

        RunnerContext MakeContext(SentenceBag query, Expression expression, CancellationToken cancellationToken = default)
            => RunnerContextFactory.Create(query, providerContext.DbContext, expression, providerContext.Parameters, cancellationToken);

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            return new ExpressionQueryImpl<TElement>( providerContext.DbContext, expression);
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
                    providerContext.DbContext, expression)!;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            using var m = ActivityService.Start(ActivityID.QueryProviderExecuteT);

            var query = GetQuery<TResult>(ref expression, false, out _);

            using (StartLoadTransaction(query))
            {
                return (TResult)query.Runner.loadElement(MakeContext(query, expression))!;
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


        SentenceBag<T> GetQuery<T>(ref Expression expression, bool cache, out bool dependsOnParameters)
        {
            dependsOnParameters = false;

            var info = QueryMate.GetQuery<T>(providerContext.DbContext, ref expression, out dependsOnParameters);

            return info;
        }

        SentenceBag GetQuery(ref Expression expression, bool cache, out bool dependsOnParameters)
        {
            dependsOnParameters = false;

            if (cache && providerContext.Info != null)
                return providerContext.Info;
            var method = typeof(QueryMate).GetMethod("GetQuery");
            var paras= new Object[] {
                providerContext.DbContext,  expression,  dependsOnParameters
            };
            var res = method.MakeGenericMethod(expression.Type)
                .Invoke(null, paras);

            return res as SentenceBag;
        }

        async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var query = GetQuery<TResult>(ref expression, false, out _);

            var value = await query.Runner.loadElementAsync(MakeContext(query, expression, cancellationToken))
                .ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

            return (TResult)value!;
        }

        IDisposable? StartLoadTransaction(SentenceBag query) => null;

#if NET6_0_OR_GREATER
        async Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var query = GetQuery<TResult>(ref expression, false, out _);
            return query.Runner.loadResultList(MakeContext(query, expression, cancellationToken));
        }
#endif

    }
}
