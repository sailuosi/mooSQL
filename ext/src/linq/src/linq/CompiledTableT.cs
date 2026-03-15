using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	using Builder;
	using Common.Internal.Cache;
    using mooSQL.data;

    sealed class CompiledTable<T>
		where T : notnull
	{
		public CompiledTable(LambdaExpression lambda, Expression expression)
		{
			_lambda     = lambda;
			_expression = expression;
		}

		readonly LambdaExpression _lambda;
		readonly Expression       _expression;

		SentenceBag<T> GetInfo(DBInstance dataContext, object?[] parameterValues)
		{
			//var configurationID = dataContext.ConfigurationID;
			//var dataOptions     = dataContext.Options;
			var opt = dataContext.dialect.Option;
			var result = QueryRunner.Cache<T>.QueryCache.GetOrCreate(
				(
					operation: "CT",
                    dataContext.config.name,
					expression : _expression,
					queryFlags : dataContext
				),
				(dataContext, lambda: _lambda, opt, parameterValues),
				(o, key, ctx) =>
				{
					o.SlidingExpiration = opt.CacheSlidingExpiration;

					var optimizationContext = new ExpressionTreeOptimizationContext(ctx.dataContext);
					var exposed = ExpressionBuilder.ExposeExpression(key.expression, ctx.dataContext,
						optimizationContext, ctx.parameterValues, optimizeConditions : false, compactBinary : true);

					//var query             = new Query<T>(ctx.dataContext, exposed);
					var parametersContext = new ParametersContext(exposed, ctx.parameterValues, optimizationContext, ctx.dataContext);

					//query = new ExpressionBuilder( false, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.lambda.Parameters.ToArray(), ctx.parameterValues)
					//	.Build<T>();

					//if (query.ErrorExpression != null)
					//{
					//	query = new Query<T>(ctx.dataContext, exposed);

					//	query = new ExpressionBuilder( true, optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.lambda.Parameters.ToArray(), ctx.parameterValues)
					//		.Build<T>();

					//	if (query.ErrorExpression != null)
					//		throw query.ErrorExpression.CreateException();
					//}

					var sentence = QueryMate.CreateQuery<T>(optimizationContext, parametersContext, ctx.dataContext, exposed, ctx.lambda.Parameters.ToArray(), ctx.parameterValues);

                    sentence.ClearDynamicQueryableInfo();
					return sentence;
				})!;

			return result;
		}

		public IQueryable<T> Create(object[] parameters, object[] preambles)
		{
			var db = (DBInstance)parameters[0];
			return new Table<T>(db, _expression) { Info = GetInfo(db, parameters), Parameters = parameters };
		}

		public T Execute(object[] parameters, object[] preambles)
		{
			var db    = (DBInstance)parameters[0];
			var query = GetInfo(db, parameters);
			var para = new RunnerContext { 
				dataContext = db,
				expression = _expression,
				paras = parameters,
				premble = preambles
			};
			return (T)query.Runner.loadElement(para)!;
		}

		public async Task<T> ExecuteAsync(object[] parameters, object[] preambles)
		{
			var db    = (DBInstance)parameters[0];
			var query = GetInfo(db, parameters);
            var para = new RunnerContext
            {
                dataContext = db,
                expression = _expression,
                paras = parameters,
                premble = preambles,
				cancellationToken= default,
            };
            return (T)(await query.Runner.loadElementAsync(para).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))!;
		}
	}
}
