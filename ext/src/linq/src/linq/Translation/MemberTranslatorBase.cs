using System;
using System.Linq.Expressions;
using mooSQL.data.model;
using mooSQL.linq;
using mooSQL.linq.expressions;
using mooSQL.linq.clause;

namespace mooSQL.linq.translator
{
	/// <summary>
	/// MemberTranslatorBase type.
	/// </summary>
	/// <summary>
	/// MemberTranslatorBase type.
	/// </summary>
	public class MemberTranslatorBase : IMemberTranslator
	{
		/// <summary>
		/// Registration
		/// </summary>
		/// <summary>
		/// Registration
		/// </summary>
		protected TranslationRegistration  Registration             = new();
		/// <summary>
		/// CombinedMemberTranslator
		/// </summary>
		/// <summary>
		/// CombinedMemberTranslator
		/// </summary>
		protected CombinedMemberTranslator CombinedMemberTranslator = new();

		/// <summary>
		/// Called before every translation attempt.
		/// </summary>
		/// <param name="translationContext"></param>
		/// <param name="memberExpression"></param>
		/// <param name="translationFlags"></param>
		/// <returns></returns>
		protected virtual Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		/// <summary>
		/// 翻译非必需表达式；若不需要翻译则返回 null。
		/// </summary>
		/// <param name="translationContext">翻译上下文。</param>
		/// <param name="objExpression">待翻译表达式。</param>
		/// <param name="translationFlags">翻译标志。</param>
		/// <param name="skipIfParameter">若为参数表达式是否跳过。</param>
		/// <returns>SQL 占位符，或 null。</returns>
		protected SqlPlaceholderExpression? TranslateNoRequiredExpression(ITranslationContext translationContext, Expression? objExpression, TranslationFlags translationFlags, bool skipIfParameter = true)
		{
			if (objExpression == null)
				return null;

			var obj = translationContext.Translate(objExpression, translationFlags);

			if (obj is not SqlPlaceholderExpression objPlaceholder)
				return null;

			if (skipIfParameter && objPlaceholder.Sql is ParameterWord)
				return null;

			return objPlaceholder;
		}
		/// <summary>
		/// Translates Translate for this dialect.
		/// </summary>

		public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			var translated = TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
			if (translated != null)
				return translated;

			if (memberExpression is MethodCallExpression { Method: { } method })
			{
				var translationFunc = Registration.GetTranslation(method);
				if (translationFunc != null)
					return translationFunc(translationContext, memberExpression, translationFlags);

			}
			else if (memberExpression is MemberExpression { Member: { } member })
			{
				var translationFunc = Registration.GetTranslation(member);
				if (translationFunc != null)
					return translationFunc(translationContext, memberExpression, translationFlags);
			}
			else if (memberExpression is NewExpression { Constructor: { } constructor })
			{
				var translationFunc = Registration.GetTranslation(constructor);
				if (translationFunc != null)
					return translationFunc(translationContext, memberExpression, translationFlags);
			}

			translated = CombinedMemberTranslator.Translate(translationContext, memberExpression, translationFlags);
			if (translated != null)
				return translated;

			translated = Registration.ProvideReplacement(memberExpression);
			if (translated != null)
				return translated;

			return null;
		}
	}
}
