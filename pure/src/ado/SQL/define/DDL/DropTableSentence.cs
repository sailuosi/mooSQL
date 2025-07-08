using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 删表
	/// </summary>
	public class DropTableSentence : BaseSentence
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDropTableSentence(this);
        }
		/// <summary>
		/// 目标表
		/// </summary>
		public ITableNode Table { get; private set; }
		public DropTableSentence(ITableNode table) : base(ClauseType.DropTableStatement, null)
        {
			Table = table;
		}



		public override QueryType        QueryType    => QueryType.DropTable;
		public override ClauseType NodeType  => ClauseType.DropTableStatement;
		public override bool             IsParameterDependent { get => false; set {} }
		public override SelectQueryClause?     SelectQuery          { get => null;  set {} }

		public void Update(ITableNode table)
		{
			Table = table;
		}

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
