namespace mooSQL.data.model
{
	/// <summary>
	/// 集合操作符  指union 类的
	/// </summary>
	public class SetOperatorWord : Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSetOperator(this);
        }
		/// <summary>
		/// 集合运算右侧子查询（UNION / EXCEPT / INTERSECT 等）。
		/// </summary>
		public SetOperatorWord(SelectQueryClause selectQuery, SetOperation operation) : base(ClauseType.SetOperator, null)
        {
			SelectQuery = selectQuery;
			Operation   = operation;
		}

		/// <summary>集合运算右侧的 SELECT 子句。</summary>
		public SelectQueryClause  SelectQuery { get; private set; }
		/// <summary>集合运算种类。</summary>
		public SetOperation Operation   { get; }

#if DEBUG
		/// <summary>调试输出文本。</summary>
		public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SetOperator;

		/// <summary>替换右侧子查询节点。</summary>
		public void Modify(SelectQueryClause selectQuery)
		{
			SelectQuery = selectQuery;
		}

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		/// <inheritdoc />
		IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendLine(" ");

			switch (Operation)
			{
				case SetOperation.Union        : writer.Append("UNION");         break;
				case SetOperation.UnionAll     : writer.Append("UNION ALL");     break;
				case SetOperation.Except       : writer.Append("EXCEPT");        break;
				case SetOperation.ExceptAll    : writer.Append("EXCEPT ALL");    break;
				case SetOperation.Intersect    : writer.Append("INTERSECT");     break;
				case SetOperation.IntersectAll : writer.Append("INTERSECT ALL"); break;
			}

			writer.AppendLine();
			writer.AppendElement(SelectQuery);

			return writer;
		}
	}
}
