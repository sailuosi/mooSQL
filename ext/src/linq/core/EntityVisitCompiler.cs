using System;
using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.linq;

namespace mooSQL.linq.translator;

/// <summary>
/// <see cref="EntityVisitFactory"/> 对应的编译器另轨实现。
/// 委托 <see cref="QueryMate.GetQuery{TResult}"/> + <see cref="SentenceExecutor"/>，与 <c>useQueryable</c> 主路径共用执行层。
/// </summary>
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
