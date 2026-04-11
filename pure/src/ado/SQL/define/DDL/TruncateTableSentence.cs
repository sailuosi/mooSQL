using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// <c>TRUNCATE TABLE</c> 语句。
	/// </summary>
	public class TruncateTableSentence : BaseSentence
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTruncateTableSentence(this);
        }

		/// <summary>构造空截断语句（再设置 <see cref="Table"/>）。</summary>
		public TruncateTableSentence() : base(ClauseType.TruncateTableStatement, null) { }
		/// <summary>目标表。</summary>
		public ITableNode? Table         { get; set; }
		/// <summary>是否重置标识列（方言相关）。</summary>
		public bool      ResetIdentity { get; set; }

		/// <inheritdoc />
		public override QueryType          QueryType    => QueryType.TruncateTable;
		/// <inheritdoc />
		public override ClauseType   NodeType  => ClauseType.TruncateTableStatement;

		/// <inheritdoc />
		public override bool               IsParameterDependent
		{
			get => false;
			set {}
		}

		/// <inheritdoc />
		public override SelectQueryClause? SelectQuery { get => null; set {}}

		/// <inheritdoc />
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
