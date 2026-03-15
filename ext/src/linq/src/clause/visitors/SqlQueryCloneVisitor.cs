using mooSQL.data.model;
using System;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitor : SqlQueryCloneVisitorBase
	{
		Func<Clause, bool>? _cloneFunc;

		public Clause Clone(Clause element, Func<Clause, bool>? cloneFunc)
		{
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_cloneFunc = null;
		}

		//protected override bool ShouldReplace(ISQLNode element)
		//{
		//	return base.ShouldReplace(element) || (_cloneFunc == null || _cloneFunc(element));
		//}

	}
}
