using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 删表
	/// </summary>
	public class TruncateTableSentence : BaseSentence
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTruncateTableSentence(this);
        }

		public TruncateTableSentence() : base(ClauseType.TruncateTableStatement, null) { }
		public ITableNode? Table         { get; set; }
		public bool      ResetIdentity { get; set; }

		public override QueryType          QueryType    => QueryType.TruncateTable;
		public override ClauseType   NodeType  => ClauseType.TruncateTableStatement;

		public override bool               IsParameterDependent
		{
			get => false;
			set {}
		}

		public override SelectQueryClause? SelectQuery { get => null; set {}}

		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("TRUNCATE TABLE ")
				.AppendElement(Table)
				.AppendLine();

			return writer;
		}


	}
}
