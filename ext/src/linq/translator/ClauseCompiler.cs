using System.Linq.Expressions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Expressions;
using mooSQL.linq.Linq;
using mooSQL.linq.Linq.Builder;

namespace mooSQL.linq.translator;

/// <summary>
/// 表达式树 → Statement（SentenceBag）编译入口。
/// </summary>
internal static class ClauseCompiler
{
    public static SentenceBag<T> Build<T>(
        bool validateSubqueries,
        ExpressionTreeOptimizationContext optimizationContext,
        ParametersContext parametersContext,
        DBInstance db,
        Expression expr,
        ParameterExpression[]? compiledParameters,
        object?[]? parameterValues)
    {
        var translator = new ClauseSqlTranslator(
            validateSubqueries, optimizationContext, parametersContext, db, expr, compiledParameters, parameterValues);

        return FinalizeBag(Compile<T>(translator, expr), translator);
    }

    public static SentenceBag<T> Compile<T>(
        ClauseSqlTranslator builder,
        Expression expression)
    {
        var buildInfo = new BuildInfo((IClauseContext?)null, expression, new SelectQueryClause());
        var session = StatementCompileSession.Create(builder, buildInfo);
        var resultExpr = session.VisitRoot(expression);

        ClausePredicateVisitor.ApplyLikePatternSubstitutes(builder.ParametersContext, builder.Expression);

        if (resultExpr is not StatementExpression stmt || stmt.BuildContext == null)
        {
            return new SentenceBag<T>
            {
                EntityType = typeof(T),
                ErrorExpression = resultExpr as SqlErrorExpression ?? expression,
                DBLive = builder.DBLive
            };
        }

        return session.Context.ToSentenceBag<T>(stmt, expression);
    }

    static SentenceBag<T> FinalizeBag<T>(SentenceBag<T> res, ClauseSqlTranslator translator)
    {
        res.DBLive = translator.DBLive;
        res.srcExp = translator.Expression;

        if (res.ErrorExpression == null && res.Sentences != null)
        {
            foreach (var q in res.Sentences)
            {
                if (translator.Tag?.Lines.Count > 0)
                    (q.Statement.Tag ??= new()).Lines.AddRange(translator.Tag.Lines);

                if (translator.SqlQueryExtensions != null)
                    (q.Statement.SqlQueryExtensions ??= new()).AddRange(translator.SqlQueryExtensions);
            }
        }

        return res;
    }
}
