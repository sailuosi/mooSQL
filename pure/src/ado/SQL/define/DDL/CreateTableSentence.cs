using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 建表
	/// </summary>
	public class CreateTableSentence : BaseSentence
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCreateTableSentence(this);
        }
		/// <summary>
		/// 目标表
		/// </summary>
		public ITableNode Table           { get; private set; }
		/// <summary>
		/// 头
		/// </summary>
		public string?         StatementHeader { get; set; }
		/// <summary>
		/// 尾
		/// </summary>
		public string? StatementFooter { get; set; }
		/// <summary>
		/// 默认可空
		/// </summary>
		public DefaultNullable DefaultNullable { get; set; }
		/// <summary>指定目标表元数据。</summary>
		public CreateTableSentence(ITableNode sqlTable) : base(ClauseType.CreateTableStatement, null)
        {
			Table = sqlTable;
		}



		/// <inheritdoc />
		public override QueryType        QueryType   => QueryType.CreateTable;
		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.CreateTableStatement;

		/// <inheritdoc />
		public override bool             IsParameterDependent
		{
			get => false;
			set {}
		}

		/// <summary>替换目标表节点。</summary>
		public void Update(ITableNode table)
		{
			Table = table;
		}

		/// <inheritdoc />
		public override SelectQueryClause? SelectQuery { get => null; set {}}

		/// <inheritdoc />
		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("CREATE TABLE ")
				.AppendElement(Table)
				.AppendLine();

			return writer;
		}



	}
}
