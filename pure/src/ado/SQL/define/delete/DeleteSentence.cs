using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 删除语句
	/// </summary>
	public class DeleteSentence : BaseSentenceWithQuery
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDeleteSentence(this);
        }
		/// <summary>
		/// 要删除的表
		/// </summary>
        public ITableNode? Table { get; set; }
        public Clause? Top { get; set; }

        public OutputClause? Output { get; set; }
        public DeleteSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.DeleteStatement, type)
        {
		}

		public DeleteSentence() : this(null)
		{
		}

		public override QueryType        QueryType   => QueryType.Delete;
		public override ClauseType NodeType => ClauseType.DeleteStatement;

		public override bool             IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}



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
