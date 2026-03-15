using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	using Builder;
	using Common;
	using Common.Internal.Cache;

    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.Data.mapper;
    using mooSQL.linq.Expressions;
    using mooSQL.linq.SqlProvider;
    using Reflection;
    using SqlQuery;
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

		#region Mapper



		#endregion

		#region Helpers

		static void FinalizeQuery(SentenceBag query)
		{
			if (query.IsFinalized)
				return;

			using var m = ActivityService.Start(ActivityID.FinalizeQuery);
			ISqlOptimizer SqlOptimizer = null; //query. dataContext.GetSqlOptimizer(query.dataContext.Options); 
            foreach (var sql in query.Sentences)
			{
				sql.Statement   = SqlOptimizer.Finalize(query.DBLive , sql.Statement);
			}

			query.IsFinalized = true;
		}

		static int EvaluateTakeSkipValue(RunnerContext context, int qn, IExpWord sqlExpr)
		{
			var parameterValues = new SqlParameterValues();
			QueryMate.SetParameters(context.sentenceBag,context.expression, context.dataContext,context.paras,context.sentenceBag.Sentences[qn], parameterValues);

			var evaluated = sqlExpr.EvaluateExpression(new EvaluateContext(parameterValues)) as int?;
			if (evaluated == null)
				throw new InvalidOperationException($"Cannot evaluate integer expression from '{sqlExpr}'.");
			return evaluated.Value;
		}


		//internal static ParameterAccessor GetParameter(Type type, IDataContext dataContext, SqlField field)
		//{
		//	Expression getter = Expression.Convert(
		//		Expression.Property(
		//			Expression.Convert(ExpressionBuilder.ExpressionParam, typeof(ConstantExpression)),
		//			ReflectionHelper.Constant.Value),
		//		type);

		//	var descriptor    = field.ColumnDescriptor;
		//	var dbValueLambda = descriptor.GetDbParamLambda();

		//	Expression? dbDataTypeExpression;

		//	var valueGetter = InternalExtensions.ApplyLambdaToExpression(dbValueLambda, getter);

		//	if (typeof(DataParameter).IsSameOrParentOf(valueGetter.Type))
		//	{
		//		dbDataTypeExpression = Expression.Call(Expression.Constant(field.ColumnDescriptor.GetDbDataType(false)),
		//			LinqExtensions.WithSetValuesMethodInfo,
		//			Expression.Property(valueGetter, Methods.LinqToDB.DataParameter.DbDataType));
		//		valueGetter          = Expression.Property(valueGetter, Methods.LinqToDB.DataParameter.Value);
		//	}
		//	else
		//	{
		//		var dbDataType       = field.ColumnDescriptor.GetDbDataType(true).WithSystemType(valueGetter.Type);
		//		dbDataTypeExpression = Expression.Constant(dbDataType);
		//	}

		//	var param = ParametersContext.CreateParameterAccessor(
		//		dataContext, valueGetter, null, dbDataTypeExpression, valueGetter, parametersExpression: null, name: field.Name.Replace('.', '_'));

		//	return param;
		//}


		#endregion

		#region SetRunQuery

		public delegate int TakeSkipDelegate(
            SentenceBag query,
			Expression    expression,
			DBInstance? dataContext,
			object?[]?    ps);

		static Func<RunnerContext, int, IResultEnumerable<T>> GetExecuteQuery<T>(
                SentenceBag<T> query,
				Func<RunnerContext, int, IResultEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Sentences.Count != 1)
				throw new InvalidOperationException();

			var selectQuery = query.Sentences[0].Statement.SelectQuery!;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.DBLive.dialect.Option.ProviderFlags.GetIsSkipSupportedFlag(select.TakeValue, select.SkipValue))
			{
				var newTakeValue = select.SkipValue;
				if (select.TakeValue != null)
				{
					newTakeValue = new BinaryWord(typeof(int), newTakeValue, "+", select.TakeValue);
				}
				else
				{
					newTakeValue = null;
				}

				var skipValue = select.SkipValue;

				select.TakeValue = newTakeValue;
				select.SkipValue = null;

				var q = queryFunc;

				queryFunc = (context, qn) =>
					new LimitResultEnumerable<T>(q(context, qn),
						EvaluateTakeSkipValue(context, qn, skipValue), null);
			}

			if (select.TakeValue != null && !query.DBLive.dialect.Option.ProviderFlags.IsTakeSupported)
			{
				var takeValue = select.TakeValue;

				var q = queryFunc;

				queryFunc = (context, qn) =>
					new LimitResultEnumerable<T>(q(context, qn),
						null, EvaluateTakeSkipValue(context, qn, takeValue));
			}

			return queryFunc;
		}

		class BasicResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly DBInstance      _dataContext;
			readonly Expression        _expression;
			readonly SentenceBag _query;
			readonly object?[]?        _parameters;
			readonly object?[]?        _preambles;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;

			public BasicResultEnumerable(
				RunnerContext     context,
				int               queryNumber,
				Mapper<T> mapper)
			{
				_dataContext =context.dataContext;
				_expression  = context.expression;
				_query       = context.sentenceBag;
				_parameters  = context.paras;
				_preambles   = context.premble;
				_queryNumber = queryNumber;
				_mapper      = mapper;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _      = ActivityService.Start(ActivityID.ExecuteQuery);

				var cmds = QueryMate.TranslateCmds(new RunnerContext
				{
					sentenceBag = _query,
					dataContext = _dataContext,
					expression = _expression,
					paras = _parameters,
					premble=_preambles
				},
					_query.Sentences[_queryNumber],false
				);
				//using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expression, _parameters, _preambles);
				//TODO 此处是否可以被pure下查询器，直接转换为结果类T? 并不需要这里的读取逻辑。
				return _dataContext.ExeQueryReader(cmds.cmds[0], (dataReader) => {
					return doReading(dataReader);
                });
			}

			private IEnumerator<T> doReading(DbDataReader dataReader) {
                if (dataReader.Read())
                {
                    DbDataReader origDataReader;

     
                        origDataReader = dataReader;
                    

                    var mapperInfo = _mapper.GetMapperInfo(_dataContext, origDataReader);
                    var traceMapping = Configuration.TraceMaterializationActivity;

                    do
                    {
                        T res;
                        var a = traceMapping ? ActivityService.Start(ActivityID.Materialization) : null;

                        try
                        {
                            res = mapperInfo.Mapper(null, origDataReader);
                            //runner.RowsCount++;
                        }
                        catch (Exception ex) when (ex is FormatException or InvalidCastException  || ex.GetType().Name.Contains("NullValueException"))
                        {
                            // TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
                            if (mapperInfo.IsFaulted)
                                throw;

                            res = _mapper.ReMapOnException(_dataContext, null, origDataReader, ref mapperInfo, ex);
                            //runner.RowsCount++;
                        }
                        finally
                        {
                            a?.Dispose();
                        }

                        yield return res;
                    }
                    while (dataReader.Read());
                }

            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
#if NET6_0_OR_GREATER
            async private IAsyncEnumerable<T> doReadingAsync(DbDataReader dataReader,CancellationToken cancellationToken)
            {


                cancellationToken.ThrowIfCancellationRequested();

                if (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
                {

                    var mapperInfo = _mapper.GetMapperInfo(_dataContext, null, dataReader);
                    var traceMapping = Configuration.TraceMaterializationActivity;

                    do
                    {
                        T res;
                        var a = traceMapping ? ActivityService.Start(ActivityID.Materialization) : null;

                        try
                        {
                            res = mapperInfo.Mapper(null, dataReader);

                        }
                        catch (Exception ex) when (ex is FormatException or InvalidCastException  || ex.GetType().Name.Contains("NullValueException"))
                        {
                            // TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
                            if (mapperInfo.IsFaulted)
                                throw;

                            res = _mapper.ReMapOnException(_dataContext, null, dataReader, ref mapperInfo, ex);

                        }
                        finally
                        {
                            a?.Dispose();
                        }

                        yield return res;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext));
                }

            }


			public async IAsyncEnumerator<T> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteQueryAsync))
				{
					var cmds = QueryMate.GetQueryCmds(_query, _dataContext, _queryNumber, _expression, _parameters, _preambles);
                    var res= _dataContext.ExeQueryReader(cmds.cmds[0], (dataReader) => {
                        return doReadingAsync(dataReader,cancellationToken);
                    });

                    await foreach (var item in res)
                    {
						yield return item;
                    }
                }
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			{
				return GetAsyncEnumerable(cancellationToken);
			}
#endif
		}



		//sealed class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
		//{
		//	readonly SentenceBag _query;
		//	readonly IDataContext      _dataContext;
		//	readonly Mapper<T>         _mapper;
		//	readonly Expression        _expression;
		//	readonly object?[]?        _ps;
		//	readonly object?[]?        _preambles;
		//	readonly int               _queryNumber;
		//	readonly TakeSkipDelegate? _skipAction;
		//	readonly TakeSkipDelegate? _takeAction;
		//	readonly CancellationToken _cancellationToken;

		//	IQueryRunner?     _queryRunner;
		//	IDataReaderAsync? _dataReader;
		//	int               _take;

		//	public AsyncEnumeratorImpl(
  //              SentenceBag query,
		//		IDataContext      dataContext,
		//		Mapper<T>         mapper,
		//		Expression        expression,
		//		object?[]?        ps,
		//		object?[]?        preambles,
		//		int               queryNumber,
		//		TakeSkipDelegate? skipAction,
		//		TakeSkipDelegate? takeAction,
		//		CancellationToken cancellationToken)
		//	{
		//		_query             = query;
		//		_dataContext       = dataContext;
		//		_mapper            = mapper;
		//		_expression        = expression;
		//		_ps                = ps;
		//		_preambles         = preambles;
		//		_queryNumber       = queryNumber;
		//		_skipAction        = skipAction;
		//		_takeAction        = takeAction;
		//		_cancellationToken = cancellationToken;
		//	}

		//	public T Current { get; set; } = default!;

		//	public async ValueTask<bool> MoveNextAsync()
		//	{
		//		if (_queryRunner == null)
		//		{
		//			_queryRunner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expression, _ps, _preambles);
		//			_dataReader  = await _queryRunner.ExecuteReaderAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

		//			var skip = _skipAction?.Invoke(_query, _expression, _dataContext, _ps) ?? 0;

		//			while (skip-- > 0)
		//			{
		//				if (!await _dataReader.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
		//					return false;
		//			}

		//			_take = _takeAction?.Invoke(_query, _expression, _dataContext, _ps) ?? int.MaxValue;
		//		}

		//		if (_take-- > 0 && await _dataReader!.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
		//		{
		//			DbDataReader dataReader;

		//			if (_dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
		//			{
		//				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
		//					dataReader = interceptor.UnwrapDataReader(_dataContext, _dataReader.DataReader);
		//			}
		//			else
		//			{
		//				dataReader = _dataReader.DataReader;
		//			}

		//			var mapperInfo = _mapper.GetMapperInfo(_dataContext, _queryRunner, dataReader);

		//			Current = _mapper.Map(_dataContext, _queryRunner, dataReader, ref mapperInfo);

		//			_queryRunner.RowsCount++;

		//			return true;
		//		}

		//		return false;
		//	}

		//	public void Dispose()
		//	{
		//		_dataReader ?.Dispose();
		//		_queryRunner?.Dispose();

		//		_queryRunner = null;
		//		_dataReader  = null;
		//	}

		//	public async ValueTask DisposeAsync()
		//	{
		//		if (_dataReader != null)
		//			await _dataReader.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);

		//		if (_queryRunner != null)
		//			await _queryRunner.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);

		//		_queryRunner = null;
		//		_dataReader  = null;
		//	}
		//}



		static void SetRunQuery<T>(
            SentenceBag<T> query,
			Expression<Func<IQueryRunner, DbDataReader, T>> expression)
		{
			var mapper   = new Mapper<T>(expression);
			var executeQuery = GetExecuteQuery<T>(query, (cont, i) => {
                return new BasicResultEnumerable<T>(cont, i, mapper);
            });



			query.Runner.whenGetResultEnumerable( (context) =>
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);
				return executeQuery(context, 0);
			});
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.DBLive);
		static readonly PropertyInfo _expressionInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.Expression);
		static readonly PropertyInfo _parametersInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.Parameters);
		static readonly PropertyInfo _preamblesInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.Preambles);

		public static readonly PropertyInfo RowsCountInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.RowsCount);

		static Expression<Func<IQueryRunner, DbDataReader, T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,DBInstance, DbDataReader, Expression,object?[]?,object?[]?,T>> expression)
		{
			var queryRunnerParam = expression.Parameters[0];
			var dataReaderParam  = expression.Parameters[2];

			var dataContextVar   = expression.Parameters[1];
			var expressionVar    = expression.Parameters[3];
			var parametersVar    = expression.Parameters[4];
			var preamblesVar     = expression.Parameters[5];

			var locals = new List<ParameterExpression>();
			var exprs  = new List<Expression>();

			SetLocal(dataContextVar, _dataContextInfo);
			SetLocal(expressionVar,  _expressionInfo);
			SetLocal(parametersVar,  _parametersInfo);
			SetLocal(preamblesVar,   _preamblesInfo);

			void SetLocal(ParameterExpression local, PropertyInfo prop)
			{
				if (expression.Body.Find(local) != null)
				{
					locals.Add(local);
					exprs. Add(Expression.Assign(local, Expression.Property(queryRunnerParam, prop)));
				}
			}

			// we can safely assume it is block expression
			if (expression.Body is not BlockExpression block)
				throw new LinqException("BlockExpression missing for mapper");

			return
				Expression.Lambda<Func<IQueryRunner, DbDataReader, T>>(
					block.Update(
						locals.Concat(block.Variables),
						exprs.Concat(block.Expressions)),
					queryRunnerParam,
					dataReaderParam);
		}

		#endregion

		#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
            SentenceBag<T> query,
			Expression<Func<IQueryRunner,DBInstance, DbDataReader, Expression,object?[]?,object?[]?,T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			SentenceBag<T> query,
			Expression<Func<IQueryRunner,DBInstance, DbDataReader, Expression,object?[]?,object?[]?,object>> expression)
		{
			FinalizeQuery(query);

			if (query.Sentences.Count != 1)
				throw new InvalidOperationException();

			var l      = WrapMapper(expression);
			var mapper = new Mapper<object>(l);

			query.Runner.whenGetElement ( (cont) => ExecuteElement(cont,  mapper));
			query.Runner.whenGetElementAsync( (cont) => ExecuteElementAsync<object?>(cont, mapper));
		}

		static T ExecuteElement<T>(
			RunnerContext   context,
			Mapper<T>      mapper)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteElement);
            var cmds = prepareRun(context);
			var cmd0 = cmds[0];


			var tar= context.dataContext.ExeQueryReader(cmd0, (dr) =>
			{
				var mapperInfo = mapper.GetMapperInfo(context.dataContext, dr);

				if (dr.Read())
				{
					var ret = mapper.Map(context.dataContext, null, dr, ref mapperInfo);
					//runner.RowsCount++;
					return ret;
				}
				return default(T);
            });

			return tar;

			
		}

		static async Task<T> ExecuteElementAsync<T>(
			RunnerContext     context,
			Mapper<object>    mapper)
		{
#if NET6_0_OR_GREATER
            await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteElementAsync))
#else
            using (ActivityService.Start(ActivityID.ExecuteElementAsync))
#endif

            {
				var cmds = QueryMate.TranslateCmds(context, context.sentenceBag.Sentences[0], false);

				//var runner = context.dataContext.GetQueryRunner(context.sentenceBag, context.dataContext, 0, context.expression, context.paras, context.premble );
				var res= context.dataContext.ExeQueryReader(cmds.cmds[0], (dr) => {
					return reading<T>(dr, context, mapper);
                });
				return res.Result;
			}
		}

		private async static Task<T> reading<T>(DbDataReader dr, RunnerContext context,Mapper<object> mapper) {
#if NET6_0_OR_GREATER
            dr.ConfigureAwait(Configuration.ContinueOnCapturedContext);
#else

#endif

            if (await dr.ReadAsync(context.cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
            {

                var mapperInfo = mapper.GetMapperInfo(context.dataContext, null, dr);
                var item = mapper.Map(context.dataContext, null, dr, ref mapperInfo);

                var ret = context.dataContext.dialect.mapping.ChangeTypeTo<T>(item);

                return ret;
            }
#if NET6_0_OR_GREATER
            return Array.Empty<T>().First();
#else
			return default(T);
#endif

        }

		#endregion

		#region ScalarQuery

		public static void SetScalarQuery(SentenceBag query)
		{
			FinalizeQuery(query);

			if (query.Sentences.Count != 1)
				throw new InvalidOperationException();

			query.Runner.whenGetElement( (context) => ScalarQuery(context));
			query.Runner.whenGetElementAsync ( (context) => ScalarQueryAsync(context));
		}

		static List<SQLCmd> prepareRun(RunnerContext context) {
			if (context == null) throw new Exception("待执行的上下文不存在！");
            if (context.sentenceBag == null) throw new Exception("待执行的SQL模型不存在！或许linq尚未翻译");
			var res= new List<SQLCmd>();
			foreach (var sentence in context.sentenceBag.Sentences) {
				if (sentence.cmds == null) {
					//执行翻译SQL
					var cmds= QueryMate.TranslateCmds(context, sentence, false);
					foreach (var cmd in cmds.cmds) { 
						res.Add(cmd);
					}
				}
			}
			return res;
        }

		static object? ScalarQuery(RunnerContext context)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalar);

			var cmds= prepareRun(context);
			return context.dataContext.ExeQueryScalar(cmds[0]);
			//return runner.ExecuteScalar();
		}

		static async Task<object?> ScalarQueryAsync(RunnerContext context)
		{
            using var m = ActivityService.Start(ActivityID.ExecuteScalarAsync);

            var cmds = prepareRun(context);
            return context.dataContext.ExeQueryScalarAsync(cmds[0],context.cancellationToken);
		}

		#endregion

		#region NonQueryQuery
		/// <summary>
		/// 核心分发点，linq执行的最终动作，在这里定义
		/// </summary>
		/// <param name="query"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public static void SetNonQueryQuery(SentenceBag query)
		{
			FinalizeQuery(query);

			if (query.Sentences.Count != 1)
				throw new InvalidOperationException();

			query.Runner.whenGetElement( (cont) => NonQueryQuery(cont));
			query.Runner.whenGetElementAsync( (cont) => NonQueryQueryAsync(cont));
		}

		static int NonQueryQuery(RunnerContext context)
		{
            using var m = ActivityService.Start(ActivityID.ExecuteNonQuery);
            var cmds = prepareRun(context);
            var cmd0 = cmds[0];
			return context.dataContext.ExeNonQuery(cmd0);
		}

		static async Task<object?> NonQueryQueryAsync(RunnerContext context)
		{
            using var m = ActivityService.Start(ActivityID.ExecuteNonQueryAsync);
            var cmds = prepareRun(context);
            var cmd0 = cmds[0];
            return context.dataContext.ExeNonQueryAsync(cmd0,context.cancellationToken);
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
			using var m      = ActivityService.Start(ActivityID.GetSqlText);
            var cmds = QueryMate.GetQueryCmds(query, dataContext, 0, expr, parameters, preambles);
			return cmds.cmds[0].sql;
            //using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
			//return runner.GetSqlText();
		}

		#endregion
	}
}
