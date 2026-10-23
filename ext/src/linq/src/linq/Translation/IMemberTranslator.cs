using System.Linq.Expressions;
using mooSQL.linq.translator;

namespace mooSQL.linq
{
	/// <summary>方言成员/方法表达式翻译器。</summary>
	public interface IMemberTranslator
	{
		/// <summary>
		/// Translates Translate for this dialect.
		/// </summary>
		/// <summary>
		/// Translates Translate for this dialect.
		/// </summary>
		Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags);
	}
}
