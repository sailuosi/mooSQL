using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	using Common;
    using mooSQL.data.model;
    using System.Xml.Linq;

    public class SqlQueryActionVisitor : ClauseVisitor
	{
		Action<ISQLNode>   _visitAction = default!;
		HashSet<ISQLNode>? _visited;

		public SqlQueryActionVisitor() 
		{
		}

		public ISQLNode Visit(ISQLNode root, bool visitAll, Action<ISQLNode> visitAction)
		{
			_visitAction = visitAction;
			_visited     = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<ISQLNode>.Default);

			return Visit(root as Clause);
		}

		public void Cleanup()
		{
			_visitAction = null!;
			_visited     = null;
		}

        public override Clause Visit(Clause element)
        {
            if (element == null)
                return null;

            if (_visited != null && _visited.Contains(element))
            {
                return element;
            }

            var result = base.Visit(element);

            _visitAction(element);
            _visited?.Add(element);

            return result;
        }


	}
}
