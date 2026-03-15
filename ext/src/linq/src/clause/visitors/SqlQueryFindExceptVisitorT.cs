using mooSQL.data.model;
using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	/// <summary>
	/// Search for element in query using search condition predicate.
	/// Do not visit provided element.
	/// </summary>
	public class SqlQueryFindExceptVisitor<TContext> : ClauseVisitor
	{
		TContext                            _context  = default!;
		Func<TContext, ISQLNode, bool> _findFunc = default!;
		ISQLNode                       _skip     = default!;
		ISQLNode?                      _found;

        public VisitMode VisitingMode;
        public SqlQueryFindExceptVisitor() 
		{
            VisitingMode = VisitMode.ReadOnly;
        }

		public ISQLNode? Find(TContext context, ISQLNode root, ISQLNode skip, Func<TContext, ISQLNode, bool> findFunc)
		{
			_context  = context;
			_findFunc = findFunc;
			_skip     = skip;
			_found    = null;

			Visit(root as Clause);

			return _found;
		}

		public void Cleanup()
		{
			_context  = default!;
			_findFunc = null!;
			_skip     = null!;
			_found    = null;
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

			if (ReferenceEquals(_skip, element))
				return element;

			return base.Visit(element);
		}
	}
}
