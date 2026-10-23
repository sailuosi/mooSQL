using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using mooSQL.linq;

namespace mooSQL.linq.translator
{
	/// <summary>
	/// CombinedMemberTranslator type.
	/// </summary>
	/// <summary>
	/// CombinedMemberTranslator type.
	/// </summary>
	public sealed class CombinedMemberTranslator : IMemberTranslator
	{
		/// <summary>
		/// CombinedMemberTranslator type.
		/// </summary>
		/// <summary>
		/// CombinedMemberTranslator type.
		/// </summary>
		public CombinedMemberTranslator()
		{
			Translators = new ();
		}
		/// <summary>
		/// CombinedMemberTranslator type.
		/// </summary>

		public CombinedMemberTranslator(IEnumerable<IMemberTranslator> translators)
		{
			Translators = translators.ToList();
		}
		/// <summary>
		/// Gets or sets Translators.
		/// </summary>

		public List<IMemberTranslator> Translators { get; set; }
		/// <summary>
		/// Translates Translate for this dialect.
		/// </summary>

		public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			foreach (var translator in Translators)
			{
				var result = translator.Translate(translationContext, memberExpression, translationFlags);
				if (result != null)
					return result;
			}

			return null;
		}
		/// <summary>
		/// Add
		/// </summary>

		public void Add(IMemberTranslator memberTranslator)
		{
			Translators.Add(memberTranslator);
		}
	}
}
