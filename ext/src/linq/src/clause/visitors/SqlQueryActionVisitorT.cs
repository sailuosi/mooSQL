using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	using Common;
    using mooSQL.data.model;

    public class SqlQueryActionVisitor<TContext> : ClauseVisitor
	{
		TContext                        _context     = default!;
		Action<TContext, ISQLNode> _visitAction = default!;
		HashSet<ISQLNode>?         _visited;

		public SqlQueryActionVisitor()
		{
		}

		public ISQLNode Visit(TContext context, ISQLNode root, bool visitAll, Action<TContext, ISQLNode> visitAction)
		{
			if (root is Clause clause) { 
				_context     = context;
				_visitAction = visitAction;

				_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<ISQLNode>.Default);

				return Visit(clause);			
			}
			return root;
		}

		public void Cleanup()
		{
			_visitAction = null!;
			_visited     = null;
			_context     = default!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		[return: NotNullIfNotNull(nameof(element))]
        public override Clause Visit(Clause element)
        {
			if (element == null)
				return null;

			if (_visited != null && _visited.Contains(element))
			{
				return element;
			}

			var result = base.Visit(element);

			_visitAction(_context, element);
			_visited?.Add(element);

			return result;
		}
	}
}
