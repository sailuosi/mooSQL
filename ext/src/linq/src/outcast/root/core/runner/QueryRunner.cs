using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	using Common;
	using Common.Internal.Cache;

    using mooSQL.data;
    using mooSQL.linq.translator;
	using Tools;


	static partial class QueryRunner
	{

        internal static readonly ConcurrentQueue<Action> CacheCleaners = new();

        /// <summary>
        /// Clears query caches for all typed queries.
        /// </summary>
        public static void ClearCaches()
        {
            foreach (var cleaner in CacheCleaners)
            {
                cleaner();
            }
        }
        public static class Cache<T>
		{
			static Cache()
			{
				CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Clear();
			}

			internal static MemoryCache<IStructuralEquatable,SentenceBag<T>> QueryCache { get; } = new(new());
		}

		public static class Cache<T,TR>
		{
			static Cache()
			{
				CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Clear();
			}

			internal static MemoryCache<IStructuralEquatable,SentenceBag<TR>> QueryCache { get; } = new(new());
		}

		#region Helpers

		internal static void FinalizeQuery(SentenceBag query)
		{
			if (query.IsFinalized)
				return;

			using var m = ActivityService.Start(ActivityID.FinalizeQuery);
			var optimizer = SqlOptimizerFactory.Get(query.DBLive);
			foreach (var sql in query.Sentences)
			{
				sql.Statement = optimizer.Finalize(query.DBLive, sql.Statement);
			}

			query.IsFinalized = true;
		}

		internal static List<SQLCmd> prepareRun(RunnerContext context) {
			if (context == null) throw new Exception("待执行的上下文不存在！");
            if (context.sentenceBag == null) throw new Exception("待执行的SQL模型不存在！或许linq尚未翻译");
			var res= new List<SQLCmd>();
			foreach (var sentence in context.sentenceBag.Sentences) {
				if (sentence.cmds == null) {
					var cmds= QueryMate.TranslateCmds(context, sentence, false);
					foreach (var cmd in cmds.cmds) { 
						res.Add(cmd);
					}
				}
			}
			return res;
        }

		#endregion

		#region NonQueryQuery2

		public static void SetNonQueryQuery2(SentenceBag query)
		{
			FinalizeQuery(query);

			if (query.Sentences.Count != 2)
				throw new InvalidOperationException();

			query.Runner.whenGetElement ( (cont)        => NonQueryQuery2(cont));
			query.Runner.whenGetElementAsync( (cont) => NonQueryQuery2Async(cont));
		}

		static int NonQueryQuery2(RunnerContext context)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery2);
			var cmds= prepareRun(context);
			var cmd0= cmds[0];
			var n= context.dataContext.ExeNonQuery(cmd0);
			if (n != 0)
				return n;

			var cmd1= cmds[1];

			return context.dataContext.ExeNonQuery(cmd1);
		}

		static async Task<object?> NonQueryQuery2Async(RunnerContext context)
		{
            using var m = ActivityService.Start(ActivityID.ExecuteNonQuery2Async);
            var cmds = prepareRun(context);
            var cmd0 = cmds[0];
            var n =await context.dataContext.ExeNonQueryAsync(cmd0,context.cancellationToken);
            if (n != 0)
                return n;

            var cmd1 = cmds[1];

            return context.dataContext.ExeNonQueryAsync(cmd1,context.cancellationToken);
		}

		#endregion

		#region QueryQuery2

		public static void SetQueryQuery2(SentenceBag query)
		{
			FinalizeQuery(query);

			if (query.Sentences.Count != 2)
				throw new InvalidOperationException();

			query.Runner.whenGetElement( (context)        => QueryQuery2(context));
			query.Runner.whenGetElementAsync( (context) => QueryQuery2Async(context));
		}

		static int QueryQuery2(RunnerContext context)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalarAlternative);
			var cmds = prepareRun(context);


			var cmd0 = cmds[0];
			var n = context.dataContext.ExeQueryScalar(cmd0);

			if (n != null)
				return 0;

			var cmd1= cmds[1];

			return context.dataContext.ExeNonQuery(cmd1);
		}

		static async Task<object?> QueryQuery2Async(RunnerContext context)
		{
            var cmds = prepareRun(context);
            var cmd0 = cmds[0];
            var n = context.dataContext.ExeQueryScalarAsync(cmd0,context.cancellationToken);

            if (n != null)
                return 0;

            var cmd1 = cmds[1];

            return context.dataContext.ExeNonQueryAsync(cmd1,context.cancellationToken);
		}

		#endregion

		#region GetSqlText

		public static string GetSqlText(SentenceBag query, DBInstance dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using var m = ActivityService.Start(ActivityID.GetSqlText);
			return SentenceExecutor.GetSqlText(query, dataContext, expr);
		}

		#endregion
	}
}
