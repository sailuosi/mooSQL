using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace mooSQL.linq.translator
{
	using mooSQL.linq.utils;
	using mooSQL.data.model;
	using mooSQL.linq.expressions;
	using mooSQL.linq.clause;
	/// <summary>
	/// TranslationContextExtensions type.
	/// </summary>
	/// <summary>
	/// TranslationContextExtensions type.
	/// </summary>
	public static class TranslationContextExtensions
	{
		/// <summary>
		/// TryEvaluate&lt;T&gt;
		/// </summary>
		/// <summary>
		/// TryEvaluate&lt;T&gt;
		/// </summary>
		public static bool TryEvaluate<T>(this ITranslationContext translationContext, Expression expression, out T result)
		{
			if (translationContext.CanBeEvaluated(expression))
			{
				var value = translationContext.Evaluate(expression);
				if (value is T t)
				{
					result = t;
					return true;
				}
			}

			result = default!;
			return false;
		}
		/// <summary>
		/// Gets GetDbDataType.
		/// </summary>

		public static DbDataType GetDbDataType(this ITranslationContext translationContext, IExpWord sqlExpression)
		{
			return QueryHelper.GetDbDataType(sqlExpression, translationContext.DBLive);
		}
		/// <summary>
		/// Creates CreatePlaceholder.
		/// </summary>

		public static SqlPlaceholderExpression CreatePlaceholder(this ITranslationContext translationContext, IExpWord sqlExpression, Expression basedOn)
		{
			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sqlExpression, basedOn);
		}
		/// <summary>
		/// Translates TranslateToSqlExpression for this dialect.
		/// </summary>

		public static bool TranslateToSqlExpression(this ITranslationContext translationContext, Expression expression, [NotNullWhen(true)] out IExpWord? translated)
		{
			var result = translationContext.Translate(expression, TranslationFlags.Sql);

			if (result is not SqlPlaceholderExpression placeholder)
			{
				translated = null;
				return false;
			}

			translated = placeholder.Sql;
			return true;
		}
	}
}
