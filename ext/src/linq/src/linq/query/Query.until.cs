
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Common.Internal;

using mooSQL.linq.DataProvider;
using mooSQL.linq.Infrastructure;

using mooSQL.linq.Linq.Builder;
using mooSQL.linq.SqlProvider;
using mooSQL.linq.SqlQuery;
using mooSQL.linq.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    /// <summary>
    /// 查询助手，输入linq表达式，然后直接获得结果
    /// </summary>
    public class QueryMate
    {
        //private static readonly QueryCache _queryCache = new();


        static QueryMate()
        {
            //CacheCleaners.Enqueue(ClearCache);
        }



        internal static SentenceBag<T> GetQuery<T>(DBInstance DB, ref Expression expr, out bool dependsOnParameters)
        {
            using var mt = ActivityService.Start(ActivityID.GetQueryTotal);

            ExpressionTreeOptimizationContext optimizationContext;

            var queryFlags = QueryFlags.None;
            SentenceBag<T>? query;
            bool useCache;

            using (ActivityService.Start(ActivityID.GetQueryFind))
            {
                using (ActivityService.Start(ActivityID.GetQueryFindExpose))
                {
                    optimizationContext = new ExpressionTreeOptimizationContext(DB);

                    // I hope fast tree optimization for unbalanced Binary Expressions. See Issue447Tests.
                    //
                    expr = optimizationContext.AggregateExpression(expr);

                    dependsOnParameters = false;

                    //if (dataContext is IExpressionPreprocessor preprocessor)
                    //    expr = preprocessor.ProcessExpression(expr);
                }

                var Opti = DB.dialect.Option;

                //useCache = !Opti.DisableQueryCache;

                //if (useCache)
                //{
                //    queryFlags = dataContext.GetQueryFlags();
                //    using (ActivityService.Start(ActivityID.GetQueryFindFind))
                //        query = _queryCache.Find(dataContext, expr, queryFlags, false);

                //    if (query != null)
                //        return query;
                //}

                // 公开表达式，调用所有需要的调用
                // 在执行之后，应该没有包含IDataContext引用的常量，没有包含ExpressionQueryImpl的常量，带有SqlQueryDependentAttribute的参数将被转换为常量
                // 没有位于常量中的LambdaExpressions，它们将被扩展并注入到tree中
                //
                var exposed = ExpressionBuilder.ExposeExpression(expr, DB, optimizationContext, null,
                    optimizeConditions: true, compactBinary: false /* binary already compacted by AggregateExpression*/);


                // simple trees do not mutate
                var isExposed = !ReferenceEquals(exposed, expr);

                expr = exposed;
                //if (isExposed && useCache)
                //{
                //    dependsOnParameters = true;

                //    queryFlags |= QueryFlags.ExpandedQuery;

                //    // search again
                //    using (ActivityService.Start(ActivityID.GetQueryFindFind))
                //        query = _queryCache.Find(dataContext, expr, queryFlags, true);

                //    if (query != null)
                //        return query;
                //}


            }

            using (var mc = ActivityService.Start(ActivityID.GetQueryCreate))
                query = CreateQuery<T>(optimizationContext, new ParametersContext(expr, null, optimizationContext, DB),
                    DB, expr,null,null);

            //if (useCache && !query.DoNotCache)
            //{
            //    // 所有非值类型表达式和参数化表达式都将转换为ConstantPlaceholderExpression。它可以防止在缓存中缓存大的引用类
            //    //
            //    query.PrepareForCaching();

            //    _queryCache.TryAdd(dataContext, query, expr, queryFlags, dataOptions);
            //}

            return query;
        }
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="optimizationContext"></param>
        /// <param name="parametersContext"></param>
        /// <param name="dataContext"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        internal static SentenceBag<T> CreateQuery<T>(ExpressionTreeOptimizationContext optimizationContext, ParametersContext parametersContext, DBInstance DB, Expression expr, ParameterExpression[]? compiledParameters,
            object?[]? parameterValues)
        {


            SentenceBag<T> query =new SentenceBag<T>();

            try
            {
                query = new ExpressionBuilder( false, optimizationContext, parametersContext, DB, expr, compiledParameters, parameterValues).doBuild<T>();
                if (query.ErrorExpression != null)
                {

                    query = new ExpressionBuilder( true, optimizationContext, parametersContext, DB, expr, compiledParameters, parameterValues).doBuild<T>();
                    if (query.ErrorExpression != null)
                        throw new Exception("表达式编译错误！");
                }
            }
            catch (Exception)
            {

                throw;
            }

            return query;
        }


        internal static void SetParameters(
    SentenceBag query, Expression expression, DBInstance? parametersContext, object?[]? parameters, SentenceItem sentence, SqlParameterValues parameterValues)
        {
            var queryContext = sentence;

            foreach (var p in queryContext.ParameterAccessors)
            {
                var providerValue = p.ValueAccessor(expression, parametersContext, parameters);

                DbDataType? dbDataType = null;

                if (providerValue is IEnumerable items && p.ItemAccessor != null)
                {
                    var values = new List<object?>();

                    foreach (var item in items)
                    {
                        values.Add(p.ItemAccessor(item));
                        dbDataType ??= p.DbDataTypeAccessor(expression, item, parametersContext, parameters);
                    }

                    providerValue = values;
                }

                dbDataType ??= p.DbDataTypeAccessor(expression, null, parametersContext, parameters);

                parameterValues.AddValue(p.SqlParameter, providerValue, p.SqlParameter.Type.WithSetValues(dbDataType.Value));
            }
        }


        internal static SentenceCmds GetCommand(DBInstance dataContext, SentenceItem query, IReadOnlyParaValues? parameterValues, bool forGetSqlText, int startIndent = 0)
        {
            bool aquiredLock = false;
            try
            {
                Monitor.Enter(query, ref aquiredLock);

                var statement = query.Statement;
                //var options = query.DataOptions ?? dataContext.Options;

                if (query.cmds != null)
                {
                    return query.cmds;
                }

                var continuousRun = query.IsContinuousRun;

                if (continuousRun)
                {
                    // query will not modify statement, release lock
                    Monitor.Exit(query);
                    aquiredLock = false;
                }



                //var sqlOptimizer = dataProvider.GetSqlOptimizer(options);
                //var sqlBuilder = dataProvider.CreateSqlBuilder(dataContext.MappingSchema, options);

                // 自定义查询的处理，允许对表达式进行修订。
                var preprocessContext = new EvaluateContext(parameterValues);
                //var newSql = dataContext.ProcessQuery(statement, preprocessContext);

                //if (!ReferenceEquals(statement, newSql))
                //{
                //    statement = newSql;
                //    statement.IsParameterDependent = true;
                //}

                if (!continuousRun)
                {
                    if (!statement.IsParameterDependent)
                    {
                        //if (sqlOptimizer.IsParameterDependent(NullabilityContext.NonQuery, statement))
                        //    statement.IsParameterDependent = true;
                    }
                }
                //构造结果对象
                var cmds = new SentenceCmds();
                cmds.Sql = statement;
                //cmds.QueryHints = dataContext.GetNextCommandHints(!forGetSqlText);

                var translator = dataContext.dialect.clauseTranslator;
                translator.Prepare(dataContext);
                //var cc = sqlBuilder.CommandCount(statement);
                using var sb = Pools.StringBuilder.Allocate();

                //var commands = new CommandWithParameters[cc];

                var optimizeAndConvertAll = !continuousRun && !statement.IsParameterDependent;
                // 我们可以一次优化和转换所有查询，因为它们不依赖于参数。

                //var optimizeVisitor = sqlOptimizer.CreateOptimizerVisitor(optimizeAndConvertAll);
                //var convertVisitor = sqlOptimizer.CreateConvertVisitor(optimizeAndConvertAll);

                //// 在优化整个查询时，不将参数值传递给评估上下文.
                //var evaluationContext = new EvaluateContext(optimizeAndConvertAll ? null : parameterValues);

                //var optimizationContext = new OptimizationContext(evaluationContext, options,
                //        dataProvider.SqlProviderFlags,
                //        dataContext.MappingSchema,
                //        optimizeVisitor,
                //        convertVisitor,
                //        dataProvider.SqlProviderFlags.IsParameterOrderDependent,
                //        isAlreadyOptimizedAndConverted: optimizeAndConvertAll,
                //        dataProvider.GetQueryParameterNormalizer);

                //if (optimizeAndConvertAll)
                //{
                //    var nullability = NullabilityContext.GetContext(statement.SelectQuery);
                //    statement = optimizationContext.OptimizeAndConvertAll(statement, nullability);
                //}

                //// correct aliases if needed
                //var serviceProvider = ((IInfrastructure<IServiceProvider>)dataProvider).Instance;
                //AliasesHelper.PrepareQueryAndAliases(serviceProvider.GetRequiredService<IIdentifierService>(), statement, query.Aliases, out var aliases);

                //query.Aliases = aliases;

                //for (var i = 0; i < cc; i++)
                //{
                //    sb.Value.Length = 0;

                //    using (ActivityService.Start(ActivityID.BuildSql))
                //        sqlBuilder.BuildSql(i, statement, sb.Value, optimizationContext, aliases, startIndent);
                //    //更改构建结果的承载体
                //    var cmd = new SQLCmd(sb.Value.ToString(), CmdConnector.SqlParameterToParas(optimizationContext.GetParameters()));
                //    cmds.cmds[i] = cmd;
                //    //commands[i] = new CommandWithParameters(sb.Value.ToString(), optimizationContext.GetParameters());
                //    optimizationContext.ClearParameters();
                //}

                cmds = translator.Translate(statement);


                if (optimizeAndConvertAll)
                {
                    query.cmds = cmds;
                    //query.Context = commands;

                    // 清理依赖，在SQL生成之后就不需要使用它们了。
                    //
                    query.Aliases = null;
                }

                query.IsContinuousRun = true;
                return cmds;
                //return new PreparedQuery(commands, statement, dataConnection.GetNextCommandHints(!forGetSqlText));
            }
            finally
            {
                if (aquiredLock)
                    Monitor.Exit(query);
            }
        }

        internal static SentenceCmds TranslateCmds(RunnerContext context,SentenceItem sentence, bool forGetSqlText)
        {
            var parameterValues = new SqlParameterValues();
            var bag = context.sentenceBag;
            SetParameters(bag, bag.finalExp, bag.DBLive, context.paras, sentence, parameterValues);
            var cmds = GetCommand(context.dataContext, sentence, parameterValues, forGetSqlText);
            return cmds;
        }

        internal static SentenceCmds GetQueryCmds(SentenceBag query, DBInstance parametersContext, int queryNumber, Expression expression, object?[]? parameters, object?[]? preambles) {
            var context = new RunnerContext
            {
                sentenceBag = query,
                dataContext = parametersContext,
                expression = expression,
                paras = parameters,
                premble = preambles
            };

            var res = TranslateCmds(context, query.Sentences[queryNumber], false);
            return res;
        }
    }
}
