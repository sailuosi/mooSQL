using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// SELECT 语句句柄，包装 <see cref="BaseSentenceWithQuery.SelectQuery"/> 及可选 WITH。
	/// </summary>
	public class SelectSentence : BaseSentenceWithQuery
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSelectSentence(this);
        }
		/// <summary>使用已有查询树构造 SELECT 语句。</summary>
		public SelectSentence(SelectQueryClause? selectQuery) : base(selectQuery, ClauseType.SelectStatement, null)
		{
		}

		/// <summary>构造空的 SELECT 语句（查询树稍后填充）。</summary>
		public SelectSentence() : base(null,ClauseType.SelectStatement, null)
        {
		}

		/// <inheritdoc />
		public override QueryType          QueryType  => QueryType.Select;
		/// <inheritdoc />
		public override ClauseType   NodeType => ClauseType.SelectStatement;

		/// <inheritdoc />
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
