using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Translation
{
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.data.translation;
    using mooSQL.linq.Expressions;

    public interface ITranslationContext
    {
        Expression Translate(Expression expression, TranslationFlags translationFlags = TranslationFlags.Sql);

        DBInstance DBLive { get; }

        ISqlExpressionFactory ExpressionFactory { get; }

        EntityColumn? CurrentColumnDescriptor { get; }
        SelectQueryClause CurrentSelectQuery { get; }
        string? CurrentAlias { get; }

        SqlPlaceholderExpression CreatePlaceholder(SelectQueryClause selectQuery, IExpWord sqlExpression, Expression basedOn);
        SqlErrorExpression CreateErrorExpression(Expression basedOn, string message);

        bool CanBeCompiled(Expression expression, TranslationFlags translationFlags);
        bool IsServerSideOnly(Expression expression, TranslationFlags translationFlags);
        bool IsPreferServerSide(Expression expression, TranslationFlags translationFlags);

        bool CanBeEvaluated(Expression expression);
        object? Evaluate(Expression expression);
        bool TryEvaluate(IExpWord expression, out object? result);
    }
}
