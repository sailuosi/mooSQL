using System.Linq.Expressions;
using mooSQL.linq.Linq.Translation;

namespace mooSQL.linq
{
	/// <summary>方言成员/方法表达式翻译器。</summary>
	public interface IMemberTranslator
	{
		Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags);
	}
}
