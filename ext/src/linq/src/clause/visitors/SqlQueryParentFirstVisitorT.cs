using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	using Common;
    using mooSQL.data.model;

    public class SqlQueryParentFirstVisitor<TContext> : ClauseVisitor
	{
		Func<TContext, ISQLNode, bool> _action  = default!;
		TContext                            _context = default!;
		HashSet<ISQLNode>?             _visited;

        public VisitMode VisitingMode;
        public SqlQueryParentFirstVisitor()
		{
            VisitingMode = VisitMode.ReadOnly;
        }

		public ISQLNode Visit(TContext context, ISQLNode root, bool visitAll, Func<TContext, ISQLNode, bool> action)
		{
			_context = context;
			_action  = action;
			_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<ISQLNode>.Default);

			return Visit(root as Clause);
		}

		public void Cleanup()
		{
			_action  = null!;
			_context = default!;
		}

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

		public override Clause VisitColumnWord(ColumnWord column)
		{
			Visit(column);
			return base.VisitColumnWord(column);
		}

	}
}
