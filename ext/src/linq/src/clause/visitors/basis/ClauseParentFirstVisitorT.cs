using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.clause
{
	using mooSQL.linq.utils;
    using mooSQL.data.model;
    /// <summary>
    /// ClauseParentFirstVisitorT type.
    /// </summary>

    public class ClauseParentFirstVisitor<TContext> : ClauseVisitor
	{
		Func<TContext, ISQLNode, bool> _action  = default!;
		TContext                            _context = default!;
		HashSet<ISQLNode>?             _visited;
        /// <summary>
        /// VisitingMode
        /// </summary>

        public VisitMode VisitingMode;
        /// <summary>
        /// ClauseParentFirstVisitor
        /// </summary>
        /// <summary>
        /// ClauseParentFirstVisitor
        /// </summary>
        public ClauseParentFirstVisitor()
		{
            VisitingMode = VisitMode.ReadOnly;
        }
		/// <summary>
		/// Visit
		/// </summary>

		public ISQLNode Visit(TContext context, ISQLNode root, bool visitAll, Func<TContext, ISQLNode, bool> action)
		{
			_context = context;
			_action  = action;
			_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<ISQLNode>.Default);

			return Visit(root as Clause);
		}
		/// <summary>
		/// Cleanup
		/// </summary>

		public void Cleanup()
		{
			_action  = null!;
			_context = default!;
		}
		/// <summary>
		/// Visit
		/// </summary>
	

		[return: NotNullIfNotNull(nameof(element))]
		public override Clause? Visit(Clause? element)
		{
			if (element == null)
				return null;

			if (_visited != null && _visited.Contains(element))
			{
				return element;
			}

			_visited?.Add(element);

			if (!_action(_context, element))
			{
				return element;
			}

			return base.Visit(element);
		}
/// <summary>
/// VisitColumnWord
/// </summary>
	

		public override Clause VisitColumnWord(ColumnWord column)
		{
			Visit(column);
			return base.VisitColumnWord(column);
		}

	}
}
