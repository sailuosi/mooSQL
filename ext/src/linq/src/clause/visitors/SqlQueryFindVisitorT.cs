using mooSQL.data.model;
using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryFindVisitor<TContext> : ClauseVisitor
	{
		TContext                            _context  = default!;
		Func<TContext, ISQLNode, bool> _findFunc = default!;
		ISQLNode?                      _found;

        public VisitMode VisitingMode;
        public SqlQueryFindVisitor() 
		{
            VisitingMode = VisitMode.ReadOnly;
        }

		public ISQLNode? Find(TContext context, ISQLNode root, Func<TContext, ISQLNode, bool> findFunc)
		{
			_context  = context;
			_findFunc = findFunc;
			_found    = null;

			Visit(root as Clause);

			return _found;
		}

		public void Cleanup()
		{
			_found    = null;
			_findFunc = null!;
			_context  = default!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override Clause? Visit(Clause? element)
		{
			if (element == null)
				return null;

			if (_found != null)
				return element;

			if (_findFunc(_context, element))
			{
				_found = element;
				return element;
			}

			return base.Visit(element);
		}
	}
}
