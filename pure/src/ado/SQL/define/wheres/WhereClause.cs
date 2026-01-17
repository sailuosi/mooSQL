using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// where 条件词组，持有 SearchCondition
	/// </summary>
	public class WhereClause : ClauseBase<WhereClause>
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitWhereClause(this);
        }

		public WhereClause(SelectQueryClause selectQuery,Type type=null) : base(selectQuery,ClauseType.WhereClause,type)
		{
			SearchCondition = new SearchConditionWord();
		}

        public WhereClause(SearchConditionWord searchCondition, Type type = null) : base(null, ClauseType.WhereClause, type)
        {
			SearchCondition = searchCondition;
		}

		public SearchConditionWord SearchCondition { get;  set; }

		public bool IsEmpty => SearchCondition.Predicates.Count == 0;

		#region IQueryElement Members

		public override ClauseType NodeType => ClauseType.WhereClause;

		public IElementWriter ToString(IElementWriter writer)
		{
			if (!IsEmpty)
			{
				writer
					//.DebugAppendUniqueId(this)
					.AppendLine()
					.AppendLine("WHERE");

				using (writer.IndentScope())
					writer.AppendElement(SearchCondition);

			}

			return writer;
		}

		#endregion

		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
