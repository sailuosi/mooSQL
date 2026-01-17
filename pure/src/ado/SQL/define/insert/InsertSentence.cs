using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 支持查询的插入语句，持有 InsertClause SelectQuery
	/// </summary>
	public class InsertSentence
		: BaseSentenceWithQuery
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInsertSentence(this);
        }

		public InsertSentence( Type type = null) : base(null, ClauseType.InsertStatement, type)
        {
		}

		public InsertSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.InsertStatement, type)
        {
		}

		public override QueryType          QueryType   => QueryType.Insert;
		public override ClauseType   NodeType => ClauseType.InsertStatement;

		#region InsertClause

		private InsertClause? _insert;
		public  InsertClause   Insert
		{
			get => _insert ??= new InsertClause();
			set => _insert = value;
		}

		internal bool HasInsert => _insert != null;

		#endregion

		#region Output

		public  OutputClause?  Output { get; set; }

		#endregion

		public override IElementWriter ToString(IElementWriter writer)
		{
			return writer
				.AppendElement(_insert)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendElement(Output);
		}


	}
}
