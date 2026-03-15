using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using mooSQL.data;

using mooSQL.linq.Linq;


namespace mooSQL.linq
{
    internal class EntityQueryCompiler : BaseQueryCompiler
    {
        public EntityQueryCompiler(DBInstance DB) : base(DB)
        {
        }

        public override Func<QueryContext, TResult> DoCompile<TResult>(Expression expression,QueryContext context)
        {

            bool depon;
            var query = QueryMate.GetQuery<TResult>(DB, ref expression,out depon);
            object?[]? Parameters=null;
            return (context) =>
            {
                var Preambles = query.InitPreambles(DB, expression, Parameters);
                if (context.cancellationToken != null)
                {
                    var AsyRes = query.Runner.loadElementAsync(new RunnerContext
                    {
                        dataContext = DB,
                        expression = expression,
                        paras = Parameters,
                        sentenceBag = query,
                        premble = Preambles
                    });
                    return (TResult)AsyRes.Result;
                }
                else { 
                
                }
                var res = query.Runner.loadElement(new RunnerContext
                {
                    dataContext = DB,
                    expression = expression,
                    paras = Parameters,
                    sentenceBag = query,
                    premble = Preambles
                });
                return (TResult)res;
            };
        }
    }
}
