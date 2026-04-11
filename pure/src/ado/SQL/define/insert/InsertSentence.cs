using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 支持查询的插入语句，持有 InsertClause SelectQuery
	/// </summary>
	public class InsertSentence
		: BaseSentenceWithQuery
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitInsertSentence(this);
        }

		/// <summary>无初始查询体。</summary>
		public InsertSentence( Type type = null) : base(null, ClauseType.InsertStatement, type)
        {
		}

		/// <summary>指定 SELECT 来源查询体。</summary>
		public InsertSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.InsertStatement, type)
        {
		}

		/// <inheritdoc />
		public override QueryType          QueryType   => QueryType.Insert;
		/// <inheritdoc />
		public override ClauseType   NodeType => ClauseType.InsertStatement;

		#region InsertClause

		private InsertClause? _insert;
		/// <summary>INSERT 目标与列值（惰性创建）。</summary>
		public  InsertClause   Insert
		{
			get => _insert ??= new InsertClause();
			set => _insert = value;
		}

		/// <summary>是否已创建 INSERT 子句。</summary>
		internal bool HasInsert => _insert != null;

		#endregion

		#region Output

		/// <summary>OUTPUT/RETURNING 子句。</summary>
		public  OutputClause?  Output { get; set; }

		#endregion

		/// <inheritdoc />
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
