using mooSQL.data.model;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class QueryElementReplacingVisitor : ClauseVisitor
	{
		IDictionary<Clause, Clause> _replacements = default!;
        Clause[]                           _toIgnore     = default!;

		public VisitMode VisitingMode { get; set; }
        public QueryElementReplacingVisitor() 
		{
			this.VisitingMode = VisitMode.Modify;
		}

		public Clause Replace(
            Clause element, 
			IDictionary<Clause, Clause> replacements,
			params Clause[]                    toIgnore)
		{
			_replacements = replacements;
			_toIgnore     = toIgnore;

			return Visit(element);
		}

		public void Cleanup()
		{
			_replacements = default!;
			_toIgnore     = default!;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override Clause? Visit(Clause? element)
		{
			if (element != null)
			{
				if (_toIgnore.Contains(element))
					return element;

				if (_replacements.TryGetValue(element, out var replacement))
					return replacement;
			}

			return base.Visit(element);
		}

		// CteClause reference not visited by main dispatcher
		public override Clause VisitCteClause(CTEClause element)
		{
			if (_replacements.TryGetValue(element, out var replacement))
				return replacement;

			return base.VisitCteClause(element);
		}

	}
}
