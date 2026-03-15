using mooSQL.data.model;
using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryFindVisitor : ClauseVisitor
	{
		Func<ISQLNode, bool> _findFunc = default!;
		ISQLNode?            _found;

        public VisitMode VisitingMode;
        public SqlQueryFindVisitor()
		{
            VisitingMode = VisitMode.ReadOnly;
        }

		public ISQLNode? Find(ISQLNode root, Func<ISQLNode, bool> findFunc)
		{
			_findFunc = findFunc;
			_found    = null;

			Visit(root as Clause);

			return _found;
		}

		public void Cleanup()
		{
			_found    = null;
			_findFunc = null!;
		}

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
