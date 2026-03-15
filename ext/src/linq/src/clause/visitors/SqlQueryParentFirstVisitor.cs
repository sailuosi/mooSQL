using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	using Common;
    using mooSQL.data.model;

    public class SqlQueryParentFirstVisitor : ClauseVisitor
	{
		Func<Clause, bool> _action = default!;
		HashSet<Clause>?   _visited;

		public VisitMode VisitingMode;
        public SqlQueryParentFirstVisitor()
        {
			VisitingMode = VisitMode.ReadOnly;

        }

		public Clause Visit(Clause root, bool visitAll, Func<Clause, bool> action)
		{
			_action  = action;
			_visited = visitAll ? null : new(Utils.ObjectReferenceEqualityComparer<Clause>.Default);

			return Visit(root);
		}
		/// <summary>
		/// 清空
		/// </summary>
		public void Cleanup()
		{
			_action  = null!;
			_visited = null;
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

			if (!_action(element))
			{
				return element;
			}

			return base.Visit(element);
		}

		//protected override IExpWord VisitSqlColumnExpression(ColumnWord column, IExpWord expression)
		//{
		//	Visit(column);
		//	return base.VisitSqlColumnExpression(column, column.Expression);
		//}
	}
}
