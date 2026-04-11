using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 删表
	/// </summary>
	public class DropTableSentence : BaseSentence
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDropTableSentence(this);
        }
		/// <summary>
		/// 目标表
		/// </summary>
		public ITableNode Table { get; private set; }
		/// <summary>指定要删除的表。</summary>
		public DropTableSentence(ITableNode table) : base(ClauseType.DropTableStatement, null)
        {
			Table = table;
		}



		/// <inheritdoc />
		public override QueryType        QueryType    => QueryType.DropTable;
		/// <inheritdoc />
		public override ClauseType NodeType  => ClauseType.DropTableStatement;
		/// <inheritdoc />
		public override bool             IsParameterDependent { get => false; set {} }
		/// <inheritdoc />
		public override SelectQueryClause?     SelectQuery          { get => null;  set {} }

		/// <summary>替换目标表节点。</summary>
		public void Update(ITableNode table)
		{
			Table = table;
		}

		/// <inheritdoc />
		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("DROP TABLE ")
				.AppendElement(Table)
				.AppendLine();

			return writer;
		}


	}
}
