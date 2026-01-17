using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 建表
	/// </summary>
	public class CreateTableSentence : BaseSentence
	{
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
		public CreateTableSentence(ITableNode sqlTable) : base(ClauseType.CreateTableStatement, null)
        {
			Table = sqlTable;
		}



		public override QueryType        QueryType   => QueryType.CreateTable;
		public override ClauseType NodeType => ClauseType.CreateTableStatement;

		public override bool             IsParameterDependent
		{
			get => false;
			set {}
		}

		public void Update(ITableNode table)
		{
			Table = table;
		}

		public override SelectQueryClause? SelectQuery { get => null; set {}}

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
