using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;



namespace mooSQL.linq
{
    internal class FastLinqCompiler : BaseQueryCompiler
    {
        public FastLinqCompiler(DBInstance DB) : base(DB)
        {
        }

        public override Func<QueryContext, TResult> DoCompile<TResult>(Expression expression, QueryContext context)
        {

            var met = new FastMethodVisitor();
            var wok = new FastExpressionTranslatVisitor(met);
            met.Buddy = wok;

            var fastContext = new FastCompileContext<TResult>();

            var kit = new SQLBuilder();
            kit.setDBInstance(DB);
            fastContext.initByBuilder( kit);
;

            met.Context = fastContext;
            wok.Context = fastContext;

            wok.Visit(expression);
            if (fastContext.onExecute != null) {
                return fastContext.onExecute;
            }

            if (fastContext.onRunQuery == null) {
                //未产生执行器时。
                return met.WhenRunQuery<TResult>(context);
            }
            return (cont)=> {
                return (TResult)fastContext.onRunQuery(cont);
            };  
        }
    }
}
