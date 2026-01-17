using System;

namespace mooSQL.data.model
{
	public class SelectSentence : BaseSentenceWithQuery
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSelectSentence(this);
        }
		public SelectSentence(SelectQueryClause? selectQuery) : base(selectQuery, ClauseType.SelectStatement, null)
		{
		}

		public SelectSentence() : base(null,ClauseType.SelectStatement, null)
        {
		}

		public override QueryType          QueryType  => QueryType.Select;
		public override ClauseType   NodeType => ClauseType.SelectStatement;

		public override IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendTag(Tag);

			if (With?.Clauses.Count > 0)
			{
				writer
					.AppendElement(With)
					.AppendLine("--------------------------");
			}

			return writer.AppendElement(SelectQuery);
		}
	}
}
