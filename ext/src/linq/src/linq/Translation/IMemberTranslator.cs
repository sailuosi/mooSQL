using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Translation
{
	public interface IMemberTranslator
	{
		Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags);
	}
}
