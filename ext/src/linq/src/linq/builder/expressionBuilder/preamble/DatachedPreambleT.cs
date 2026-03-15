using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq.Builder
{
	using Async;
	using Common;
	using Common.Internal;
	using Extensions;
	using Reflection;
	using SqlQuery;
	using mooSQL.linq.Expressions;
	class DatachedPreamble<T> : Preamble
	{
		readonly SentenceBag<T> _query;

		public DatachedPreamble(SentenceBag<T> query)
		{
			_query = query;
		}
        //IDataContext dataContext, Expression expression, object?[]? parameters, object?[]? preambles
        public override object Execute(RunnerContext context)
		{
			return _query.Runner.loadResultList(context).ToList();
		}

		public override async Task<object> ExecuteAsync(RunnerContext context)
		{
#if NET6_0_OR_GREATER
            return await _query.Runner.loadResultList(context)
				.ToListAsync(context.cancellationToken)
				.ConfigureAwait(Configuration.ContinueOnCapturedContext);
#else
            return  _query.Runner.loadResultList(context).ToList();
#endif

        }
	}
}
