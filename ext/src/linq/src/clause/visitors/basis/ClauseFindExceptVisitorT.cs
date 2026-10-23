using mooSQL.data.model;
using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// Search for element in query using search condition predicate.
	/// Do not visit provided element.
	/// </summary>
	public class ClauseFindExceptVisitor<TContext> : ClauseVisitor
	{
		TContext                            _context  = default!;
		Func<TContext, ISQLNode, bool> _findFunc = default!;
		ISQLNode                       _skip     = default!;
		ISQLNode?                      _found;
        /// <summary>
        /// VisitingMode
        /// </summary>

        public VisitMode VisitingMode;
        /// <summary>
        /// ClauseFindExceptVisitor
        /// </summary>
        /// <summary>
        /// ClauseFindExceptVisitor
        /// </summary>
        public ClauseFindExceptVisitor() 
		{
            VisitingMode = VisitMode.ReadOnly;
        }
		/// <summary>
		/// Find
		/// </summary>

		public ISQLNode? Find(TContext context, ISQLNode root, ISQLNode skip, Func<TContext, ISQLNode, bool> findFunc)
		{
			_context  = context;
			_findFunc = findFunc;
			_skip     = skip;
			_found    = null;

			Visit(root as Clause);

			return _found;
		}
		/// <summary>
		/// Cleanup
		/// </summary>

		public void Cleanup()
		{
			_context  = default!;
			_findFunc = null!;
			_skip     = null!;
			_found    = null;
		}
		/// <summary>
		/// Visit
		/// </summary>
	

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
