using mooSQL.data.model;
using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// ClauseFindVisitor type.
	/// </summary>
	/// <summary>
	/// ClauseFindVisitor type.
	/// </summary>
	public class ClauseFindVisitor : ClauseVisitor
	{
		Func<ISQLNode, bool> _findFunc = default!;
		ISQLNode?            _found;
        /// <summary>
        /// VisitingMode
        /// </summary>

        public VisitMode VisitingMode;
        /// <summary>
        /// ClauseFindVisitor type.
        /// </summary>
        /// <summary>
        /// ClauseFindVisitor type.
        /// </summary>
        public ClauseFindVisitor()
		{
            VisitingMode = VisitMode.ReadOnly;
        }
		/// <summary>
		/// Find
		/// </summary>

		public ISQLNode? Find(ISQLNode root, Func<ISQLNode, bool> findFunc)
		{
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

			if (_findFunc(element))
			{
				_found = element;
				return element;
			}

			return base.Visit(element);
		}
	}
}
