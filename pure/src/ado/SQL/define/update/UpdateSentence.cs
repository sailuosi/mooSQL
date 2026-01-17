using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// update 语句
	/// </summary>
	public class UpdateSentence : BaseSentenceWithQuery
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitUpdateSentence(this);
        }

		public override QueryType QueryType          => QueryType.Update;
		public override ClauseType NodeType => ClauseType.UpdateStatement;

		public OutputClause? Output { get; set; }

		private UpdateClause? _update;

		public UpdateClause Update
		{
			get => _update ??= new UpdateClause();
			set => _update = value;
		}

		internal bool HasUpdate => _update != null;


		public UpdateSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.UpdateStatement, type)
        {
		}

		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.AppendLine("UPDATE")
				.AppendElement(Update)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendElement(Output);

			return writer;
		}

	}
}
