using System.Collections.Generic;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitorBase : SqlQueryVisitor
	{
		public SqlQueryCloneVisitorBase() : base(VisitMode.Transform, null)
		{
		}

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

		public Clause PerformClone(Clause element)
		{
			return ProcessElement(element);
		}
	}
}
