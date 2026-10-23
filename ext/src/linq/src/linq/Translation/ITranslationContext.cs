using System;
using System.Linq.Expressions;

namespace mooSQL.linq.translator
{
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.data.translation;
    using mooSQL.linq.expressions;
    /// <summary>
    /// ITranslationContext interface.
    /// </summary>

    public interface ITranslationContext
    {
        /// <summary>
        /// Translates Translate for this dialect.
        /// </summary>
        /// <summary>
        /// Translates Translate for this dialect.
        /// </summary>
        Expression Translate(Expression expression, TranslationFlags translationFlags = TranslationFlags.Sql);
        /// <summary>
        /// Gets or sets DBLive.
        /// </summary>

        DBInstance DBLive { get; }
        /// <summary>
        /// Gets or sets ExpressionFactory.
        /// </summary>

        ISqlExpressionFactory ExpressionFactory { get; }
        /// <summary>
        /// Gets or sets CurrentColumnDescriptor.
        /// </summary>

        EntityColumn? CurrentColumnDescriptor { get; }
        /// <summary>
        /// CurrentSelectQuery
        /// </summary>
        /// <summary>
        /// Gets or sets CurrentSelectQuery.
        /// </summary>
        SelectQueryClause CurrentSelectQuery { get; }
        /// <summary>
        /// CurrentAlias
        /// </summary>
        /// <summary>
        /// Gets or sets CurrentAlias.
        /// </summary>
        string? CurrentAlias { get; }
        /// <summary>
        /// Creates CreatePlaceholder.
        /// </summary>

        SqlPlaceholderExpression CreatePlaceholder(SelectQueryClause selectQuery, IExpWord sqlExpression, Expression basedOn);
        /// <summary>
        /// Creates CreateErrorExpression.
        /// </summary>
        /// <summary>
        /// Creates CreateErrorExpression.
        /// </summary>
        SqlErrorExpression CreateErrorExpression(Expression basedOn, string message);
        /// <summary>
        /// CanBeCompiled
        /// </summary>

        bool CanBeCompiled(Expression expression, TranslationFlags translationFlags);
        /// <summary>
        /// IsServerSideOnly
        /// </summary>
        /// <summary>
        /// IsServerSideOnly
        /// </summary>
        bool IsServerSideOnly(Expression expression, TranslationFlags translationFlags);
        /// <summary>
        /// IsPreferServerSide
        /// </summary>
        /// <summary>
        /// IsPreferServerSide
        /// </summary>
        bool IsPreferServerSide(Expression expression, TranslationFlags translationFlags);
        /// <summary>
        /// CanBeEvaluated
        /// </summary>

        bool CanBeEvaluated(Expression expression);
        /// <summary>
        /// Evaluate
        /// </summary>
        /// <summary>
        /// Evaluate
        /// </summary>
        object? Evaluate(Expression expression);
        /// <summary>
        /// TryEvaluate
        /// </summary>
        /// <summary>
        /// TryEvaluate
        /// </summary>
        bool TryEvaluate(IExpWord expression, out object? result);
    }
}
