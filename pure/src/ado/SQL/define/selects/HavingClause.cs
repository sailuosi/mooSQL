namespace mooSQL.data.model
{
	/// <summary>
	/// having 词组，持有条件 SearchCondition
	/// </summary>
	public class HavingClause: ClauseBase<HavingClause>
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitHavingClause(this);
        }
		public HavingClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.HavingClause, type)
        {
			SearchCondition = new SearchConditionWord();
		}

        public HavingClause(SearchConditionWord searchCondition, Type type = null) : base(null, ClauseType.HavingClause, type)
        {
			SearchCondition = searchCondition;
		}

		public SearchConditionWord SearchCondition { get; set; }

		public bool IsEmpty => SearchCondition.Predicates.Count == 0;

		#region IQueryElement Members

		public override ClauseType NodeType => ClauseType.HavingClause;

		public IElementWriter ToString(IElementWriter writer)
		{
			if (!IsEmpty)
			{
				writer
					.DebugAppendUniqueId(this)
					.AppendLine()
					.AppendLine("HAVING");

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
