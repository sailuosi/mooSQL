using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 删除语句
	/// </summary>
	public class DeleteSentence : BaseSentenceWithQuery
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDeleteSentence(this);
        }
		/// <summary>
		/// 要删除的表
		/// </summary>
        public ITableNode? Table { get; set; }
        /// <summary>TOP/LIMIT 类限制子句（方言相关）。</summary>
        public Clause? Top { get; set; }

        /// <summary>OUTPUT/RETURNING 子句（方言相关）。</summary>
        public OutputClause? Output { get; set; }
        /// <summary>使用可选查询体与实体 CLR 类型构造。</summary>
        public DeleteSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.DeleteStatement, type)
        {
		}

        /// <summary>无查询体的删除语句。</summary>
		public DeleteSentence() : this(null)
		{
		}

		/// <inheritdoc />
		public override QueryType        QueryType   => QueryType.Delete;
		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.DeleteStatement;

		/// <inheritdoc />
		public override bool             IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}



		/// <inheritdoc />
		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.Append("DELETE FROM ")
				.AppendElement(Table)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendLine()
				.AppendElement(Output);

			return writer;
		}

	}
}
