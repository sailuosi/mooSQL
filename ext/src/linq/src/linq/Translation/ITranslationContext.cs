using System;
using System.Linq.Expressions;

using mooSQL.linq.Mapping;

namespace mooSQL.linq.Linq.Translation
{
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.Expressions;


	[Flags]
	public enum TranslationFlags
	{
		None = 0,
		Expression = 1,
		Sql        = 1 << 1,
	}

	public interface ITranslationContext
	{
		Expression Translate(Expression expression, TranslationFlags translationFlags = TranslationFlags.Sql);

		public DBInstance DBLive {  get; }


		public ISqlExpressionFactory ExpressionFactory { get; }

		public EntityColumn? CurrentColumnDescriptor { get; }
		public SelectQueryClause       CurrentSelectQuery      { get; }
		public string?           CurrentAlias            { get; }

		SqlPlaceholderExpression CreatePlaceholder(SelectQueryClause selectQuery, IExpWord sqlExpression, Expression basedOn);
		SqlErrorExpression CreateErrorExpression(Expression basedOn, string message);

		public bool CanBeCompiled(Expression      expression, TranslationFlags translationFlags);
		public bool IsServerSideOnly(Expression   expression, TranslationFlags translationFlags);
		public bool IsPreferServerSide(Expression expression, TranslationFlags translationFlags);

		bool    CanBeEvaluated(Expression  expression);
		object? Evaluate(Expression        expression);
		bool    TryEvaluate(IExpWord expression, out object? result);
	}
}
