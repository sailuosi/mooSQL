using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

using mooSQL.linq.Linq;
using mooSQL.linq.translator;

namespace mooSQL.linq.core
{

    internal class EntityVisitCompiler : BaseQueryCompiler
    {
        public EntityVisitCompiler(DBInstance DB) : base(DB)
        {
        }

        public override Func<QueryContext, TResult> DoCompile<TResult>(Expression expression, QueryContext context)
        {
            //var md = new ClauseMethodVisitor();
            //var visitor = new ClauseTranslateVisitor(md);
            //var exp= visitor.Visit(expression);


            bool depon;
            var query = QueryMate.GetQuery<TResult>(DB, ref expression, out depon);
            object?[]? Parameters = null;
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
                else
                {

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
