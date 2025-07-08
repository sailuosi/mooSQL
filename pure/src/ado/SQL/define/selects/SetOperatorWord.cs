namespace mooSQL.data.model
{
	/// <summary>
	/// 集合操作符  指union 类的
	/// </summary>
	public class SetOperatorWord : Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSetOperator(this);
        }
		public SetOperatorWord(SelectQueryClause selectQuery, SetOperation operation) : base(ClauseType.SetOperator, null)
        {
			SelectQuery = selectQuery;
			Operation   = operation;
		}

		public SelectQueryClause  SelectQuery { get; private set; }
		public SetOperation Operation   { get; }

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.SetOperator;

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
