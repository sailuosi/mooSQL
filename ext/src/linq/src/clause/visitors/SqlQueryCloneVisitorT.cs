using mooSQL.data.model;
using System;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitor<TContext> : SqlQueryCloneVisitorBase

	{
		TContext                            _context   = default!;
		Func<TContext, Clause, bool> _cloneFunc = default!;

		public Clause Clone(Clause element, TContext context, Func<TContext, Clause, bool> cloneFunc)
		{
			_context   = context;
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_cloneFunc = null!;
			_context   = default!;
		}

		//protected override bool ShouldReplace(ISQLNode element)
		//{
		//	if (base.ShouldReplace(element) && _cloneFunc(_context, element))
		//	{
		//		return true;
		//	}

		//	return false;
		//}

	}
}
