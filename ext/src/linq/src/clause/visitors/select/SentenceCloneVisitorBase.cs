using System.Collections.Generic;

using mooSQL.data.model;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// SentenceCloneVisitorBase type.
	/// </summary>
	/// <summary>
	/// SentenceCloneVisitorBase type.
	/// </summary>
	public class SentenceCloneVisitorBase : SentenceVisitor
	{
		/// <summary>
		/// SentenceCloneVisitorBase type.
		/// </summary>
		/// <summary>
		/// SentenceCloneVisitorBase type.
		/// </summary>
		public SentenceCloneVisitorBase() : base(VisitMode.Transform, null)
		{
		}
		/// <summary>
		/// RegisterReplacements
		/// </summary>

		public void RegisterReplacements(IReadOnlyDictionary<Clause, Clause> replacements)
		{
			AddReplacements(replacements);
		}

		//protected override bool ShouldReplace(Clause element)
		//{
		//	if (base.ShouldReplace(element))
		//		return true;

		//	if (element.NodeType == ClauseType.SqlParameter)
		//		return false;

		//	return true;
		//}
		/// <summary>
		/// PerformClone
		/// </summary>

		public Clause PerformClone(Clause element)
		{
			return ProcessElement(element);
		}
	}
}
