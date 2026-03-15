using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace mooSQL.linq.SqlQuery
{
    using mooSQL.data.model;
    using Visitors;

	public class SqlQueryColumnNestingCorrector : SqlQueryVisitor
	{
		[DebuggerDisplay("QN(S:{TableSource.SourceID})")]
		class QueryNesting
		{
			public QueryNesting(QueryNesting? parent, ITableNode tableSource)
			{
				TableSource = tableSource;
				Parent      = parent;
				if (parent != null)
					parent.AddSource(this);
			}

			public QueryNesting?       Parent      { get; }
			public ITableNode     TableSource { get; }
			public List<QueryNesting>? Sources     { get; private set; }

			void AddSource(QueryNesting source)
			{
				Sources ??= new();

				if (!Sources.Contains(source))
					Sources.Add(source);
			}

			public QueryNesting? FindNesting(ITableNode tableSource)
			{
				if (Sources != null)
				{
					foreach (var s in Sources)
					{
						if (s.TableSource == tableSource)
							return this;
						var result = s.FindNesting(tableSource);
						if (result != null) 
							return result;
					}
				}

				return null;
			}

			public static bool UpdateNesting(QueryNesting upTo, QueryNesting current, Clause element, out Clause newElement)
			{
				while (upTo != current)
				{
					newElement = current.UpdateNesting(element);

					if (current.Parent == null)
						throw new InvalidOperationException("Invalid nesting tree.");

					current = current.Parent;
					element = newElement;
				}

				newElement = element;
				return true;
			}

			public Clause UpdateNesting(Clause element)
			{
				if (TableSource is SelectQueryClause selectQuery)
				{
					return selectQuery.Select.AddColumn(element as IExpWord);
				}

				return element;
			}

		}

		QueryNesting?      _parentQueryNesting;

		public bool HasSelectQuery { get; private set; }

		public SqlQueryColumnNestingCorrector() : base(VisitMode.Modify, null)
		{
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_parentQueryNesting = null;
		}

		public Clause CorrectColumnNesting(Clause element)
		{
			Cleanup();
			HasSelectQuery = false;
			var result = Visit(element);
			return result;
		}

        Clause ProcessNesting(ITableNode elementSource, Clause element)
		{
			if (_parentQueryNesting == null)
				return element;

			var current = _parentQueryNesting;
			while (current != null)
			{
				var found = current.FindNesting(elementSource);

				if (found != null)
				{
					if (!QueryNesting.UpdateNesting(current, found, element, out var newElement))
						throw new InvalidOperationException();

#if DEBUG
					if (!ReferenceEquals(newElement, element))
					{
						Debug.WriteLine($"Corrected nesting: {element} -> {newElement}");
					}
#endif
					return newElement;
				}

				current = current.Parent;
			}

			return element;
		}

        public override Clause VisitFieldWord(FieldWord element)
		{
			var newElement = base.VisitFieldWord(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Table != null)
			{
				newElement = ProcessNesting(element.Table, element);
			}

			return newElement;
		}

		public override Clause VisitColumnWord(ColumnWord element)
		{
			var newElement = base.VisitColumnWord(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Parent != null)
			{
				newElement = ProcessNesting(element.Parent, element);
			}

			return newElement;
		}

        public override Clause VisitSelectQuery(SelectQueryClause selectQuery)
		{
			HasSelectQuery = true;

			var saveQueryNesting = _parentQueryNesting;

			_parentQueryNesting = new QueryNesting(saveQueryNesting, selectQuery);

			var newQuery = base.VisitSelectQuery(selectQuery);

			_parentQueryNesting = saveQueryNesting;

			return newQuery;
		}

        public override Clause VisitTableSource(TableSourceWord element)
		{
			_ = new QueryNesting(_parentQueryNesting, element.Source);

			var newElement = base.VisitTableSource(element);

			return newElement;
		}

	}
}
