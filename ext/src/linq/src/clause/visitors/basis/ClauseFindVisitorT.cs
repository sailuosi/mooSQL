using mooSQL.data.model;
using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// ClauseFindVisitorT type.
	/// </summary>
	/// <summary>
	/// ClauseFindVisitorT type.
	/// </summary>
	public class ClauseFindVisitor<TContext> : ClauseVisitor
	{
		TContext                            _context  = default!;
		Func<TContext, ISQLNode, bool> _findFunc = default!;
		ISQLNode?                      _found;
        /// <summary>
        /// VisitingMode
        /// </summary>

        public VisitMode VisitingMode;
        /// <summary>
        /// ClauseFindVisitor
        /// </summary>
        /// <summary>
        /// ClauseFindVisitor
        /// </summary>
        public ClauseFindVisitor() 
		{
            VisitingMode = VisitMode.ReadOnly;
        }
		/// <summary>
		/// Find
		/// </summary>

		public ISQLNode? Find(TContext context, ISQLNode root, Func<TContext, ISQLNode, bool> findFunc)
		{
			_context  = context;
			_findFunc = findFunc;
			_found    = null;

			Visit(root as Clause);

			return _found;
		}
		/// <summary>
		/// Cleanup
		/// </summary>

		public void Cleanup()
		{
			_found    = null;
			_findFunc = null!;
			_context  = default!;
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

			return base.Visit(element);
		}
	}
}
