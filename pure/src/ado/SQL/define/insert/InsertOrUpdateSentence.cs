using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 插入或更新，含有 Insert Update 2个clause
	/// </summary>
	public class InsertOrUpdateSentence
		: BaseSentenceWithQuery
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInsertOrUpdateSentence(this);
        }
		/// <inheritdoc />
		public override QueryType QueryType          => QueryType.InsertOrUpdate;
		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.InsertOrUpdateStatement;

		private InsertClause? _insert;
		/// <summary>INSERT 部分（惰性创建）。</summary>
		public  InsertClause   Insert
		{
			get => _insert ??= new InsertClause();
			set => _insert = value;
		}

		private UpdateClause? _update;
		/// <summary>UPDATE 部分（惰性创建）。</summary>
		public  UpdateClause   Update
		{
			get => _update ??= new UpdateClause();
			set => _update = value;
		}

		/// <summary>是否已显式创建 INSERT 子句。</summary>
		internal bool HasInsert => _insert != null;
		/// <summary>是否已显式创建 UPDATE 子句。</summary>
		internal bool HasUpdate => _update != null;

		/// <summary>使用可选查询体构造。</summary>
		public InsertOrUpdateSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.InsertOrUpdateStatement, type)
        {
		}

		/// <inheritdoc />
		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.AppendLine("/* insert or update */")
				.AppendElement(Insert)
				.AppendElement(Update);
			return writer;
		}


	}
}
