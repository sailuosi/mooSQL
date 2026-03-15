
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
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class EntityProvider: IQueryProviderAsync
    {

        public ProviderContext providerContext;

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
                providerContext.Preambles = query.InitPreambles(providerContext.DbContext, expression, providerContext.Parameters);

                var para = new RunnerContext()
                {
                    dataContext = providerContext.DbContext,
                    expression = expression,
                    paras = providerContext.Parameters,
                    premble = providerContext.Preambles
                };

                return (TResult)query.Runner.loadElement(para)!;
            }
        }

        object? IQueryProvider.Execute(Expression expression)
        {
            using var m = ActivityService.Start(ActivityID.QueryProviderExecute);

            var query = GetQuery(ref expression, false, out _);

            using (StartLoadTransaction(query))
            {
                providerContext.Preambles = query.InitPreambles(providerContext.DbContext, expression, providerContext.Parameters);
                var para = new RunnerContext()
                {
                    dataContext = providerContext.DbContext,
                    expression = expression,
                    paras = providerContext.Parameters,
                    premble = providerContext.Preambles
                };

                return query.Runner.loadElement(para);
            }
        }


        SentenceBag<T> GetQuery<T>(ref Expression expression, bool cache, out bool dependsOnParameters)
        {
            dependsOnParameters = false;

            //if (cache && providerContext.Info != null)
            //    return providerContext.Info;

            var info = QueryMate.GetQuery<T>(providerContext.DbContext, ref expression, out dependsOnParameters);

            //if (cache && info.IsFastCacheable && !dependsOnParameters)
            //    providerContext.Info = info;

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


            //if (cache && info.IsFastCacheable && !dependsOnParameters)
            //    providerContext.Info = info;

            return res as SentenceBag;
        }

        private MethodInfo GenericQueryMethod
            => typeof( QueryMate)
            .GetMethod("GetQuery")!;


        async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var query = GetQuery<TResult>(ref expression, false, out _);

            //var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
            //await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

            providerContext.Preambles = await query.InitPreamblesAsync(providerContext.DbContext, expression, providerContext.Parameters, cancellationToken)
                .ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

            var para = new RunnerContext()
            {
                dataContext = providerContext.DbContext,
                expression = expression,
                paras = providerContext.Parameters,
                premble = providerContext.Preambles,
                cancellationToken = cancellationToken
            };

            var value = await query.Runner.loadElementAsync(para)
                .ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

            return (TResult)value!;
        }

        IDisposable? StartLoadTransaction(SentenceBag query)
        {
            // Do not start implicit transaction if there is no preambles
            //
            if (!query.IsAnyPreambles())
                return null;


            if (providerContext.DbContext is DBInstance ctx)
                return ctx!.beginTransaction();

            return null;
        }
#if NET6_0_OR_GREATER
        async Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var query = GetQuery<TResult>(ref expression, false, out _);

            //var transaction = await StartLoadTransactionAsync(query, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
            //await using var tr = (transaction ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

            providerContext.Preambles = await query.InitPreamblesAsync(providerContext.DbContext, expression, providerContext.Parameters, cancellationToken)
                .ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

            var sql = QueryMate.GetQuery<TResult>(providerContext.DbContext, ref expression, out _);
            var para = new RunnerContext()
            {
                dataContext = providerContext.DbContext,
                expression = expression,
                paras = providerContext.Parameters,
                premble = providerContext.Preambles
            };
            return sql.Runner.loadResultList(para);

        }
#else

#endif

    }
}
