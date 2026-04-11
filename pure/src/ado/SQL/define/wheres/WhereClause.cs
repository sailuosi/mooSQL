using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// where 条件词组，持有 SearchCondition
	/// </summary>
	public class WhereClause : ClauseBase<WhereClause>
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitWhereClause(this);
        }

		/// <summary>
		/// 挂在指定查询上的 WHERE 子句，内部新建空的 <see cref="SearchConditionWord"/>。
		/// </summary>
		public WhereClause(SelectQueryClause selectQuery,Type type=null) : base(selectQuery,ClauseType.WhereClause,type)
		{
			SearchCondition = new SearchConditionWord();
		}

		/// <summary>
		/// 使用已有搜索条件构造 WHERE 子句。
		/// </summary>
        public WhereClause(SearchConditionWord searchCondition, Type type = null) : base(null, ClauseType.WhereClause, type)
        {
			SearchCondition = searchCondition;
		}

		/// <summary>本 WHERE 的谓词组合（AND/OR）。</summary>
		public SearchConditionWord SearchCondition { get;  set; }

		/// <summary>是否尚未包含任何谓词。</summary>
		public bool IsEmpty => SearchCondition.Predicates.Count == 0;

		#region IQueryElement Members

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.WhereClause;

		/// <inheritdoc />
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

		/// <summary>清空 <see cref="SearchCondition"/> 中的谓词列表。</summary>
		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
