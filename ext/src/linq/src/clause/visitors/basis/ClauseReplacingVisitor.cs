using mooSQL.data.model;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// 置换访问者
	/// </summary>
	public class ClauseReplacingVisitor : ClauseVisitor
	{
		IDictionary<Clause, Clause> _replacements = default!;
        Clause[]                           _toIgnore     = default!;
		/// <summary>
		/// Gets or sets VisitingMode.
		/// </summary>

		public VisitMode VisitingMode { get; set; }
        /// <summary>
        /// ClauseReplacingVisitor type.
        /// </summary>
        /// <summary>
        /// ClauseReplacingVisitor type.
        /// </summary>
        public ClauseReplacingVisitor() 
		{
			this.VisitingMode = VisitMode.Modify;
		}
		/// <summary>
		/// Replace
		/// </summary>

		public Clause Replace(
            Clause element, 
			IDictionary<Clause, Clause> replacements,
			params Clause[]                    toIgnore)
		{
			_replacements = replacements;
			_toIgnore     = toIgnore;

			return Visit(element);
		}
		/// <summary>
		/// Cleanup
		/// </summary>

		public void Cleanup()
		{
			_replacements = default!;
			_toIgnore     = default!;
		}
		/// <summary>
		/// Visit
		/// </summary>
	

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
	/// <summary>
	/// VisitCteClause
	/// </summary>
	
		public override Clause VisitCteClause(CTEClause element)
		{
			if (_replacements.TryGetValue(element, out var replacement))
				return replacement;

			return base.VisitCteClause(element);
		}

	}
}
