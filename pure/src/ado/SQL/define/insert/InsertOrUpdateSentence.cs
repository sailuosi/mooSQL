using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 插入或更新，含有 Insert Update 2个clause
	/// </summary>
	public class InsertOrUpdateSentence
		: BaseSentenceWithQuery
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInsertOrUpdateSentence(this);
        }
		public override QueryType QueryType          => QueryType.InsertOrUpdate;
		public override ClauseType NodeType => ClauseType.InsertOrUpdateStatement;

		private InsertClause? _insert;
		public  InsertClause   Insert
		{
			get => _insert ??= new InsertClause();
			set => _insert = value;
		}

		private UpdateClause? _update;
		public  UpdateClause   Update
		{
			get => _update ??= new UpdateClause();
			set => _update = value;
		}

		internal bool HasInsert => _insert != null;
		internal bool HasUpdate => _update != null;

		public InsertOrUpdateSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.InsertOrUpdateStatement, type)
        {
		}

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
