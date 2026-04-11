namespace mooSQL.data.model
{
	/// <summary>
	/// having 词组，持有条件 SearchCondition
	/// </summary>
	public class HavingClause: ClauseBase<HavingClause>
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitHavingClause(this);
        }
		/// <summary>绑定查询体并初始化空条件。</summary>
		public HavingClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.HavingClause, type)
        {
			SearchCondition = new SearchConditionWord();
		}

        /// <summary>使用已有搜索条件。</summary>
        public HavingClause(SearchConditionWord searchCondition, Type type = null) : base(null, ClauseType.HavingClause, type)
        {
			SearchCondition = searchCondition;
		}

		/// <summary>聚合后的过滤条件。</summary>
		public SearchConditionWord SearchCondition { get; set; }

		/// <summary>是否尚未添加谓词。</summary>
		public bool IsEmpty => SearchCondition.Predicates.Count == 0;

		#region IQueryElement Members

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.HavingClause;

		/// <inheritdoc />
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

		/// <summary>清空谓词集合。</summary>
		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
