using System;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.linq;

namespace mooSQL.linq.translator;

internal class EntityVisitCompiler : BaseQueryCompiler
{
    public EntityVisitCompiler(DBInstance DB) : base(DB)
    {
    }

    public override Func<QueryContext, TResult> DoCompile<TResult>(Expression expression, QueryContext context)
    {
        var query = QueryMate.GetQuery<TResult>(DB, ref expression, out _);
        query.DBLive = DB;
        query.srcExp = expression;

        return ctx =>
        {
            ctx.DB ??= DB;
            return SentenceExecutor.Execute<TResult>(query, ctx, expression);
        };
    }
}
