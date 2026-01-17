namespace mooSQL.data.model
{
	public abstract class ClauseBase : SQLElement
	{
		protected ClauseBase(SelectQueryClause? selectQuery, ClauseType clauseType, Type type) : base(clauseType, type)
        {
			SelectQuery = selectQuery!;
		}

		public SelectClause  Select  => SelectQuery.Select;
		public FromClause    From    => SelectQuery.From;
		public WhereClause   Where   => SelectQuery.Where;
		public GroupByClause GroupBy => SelectQuery.GroupBy;
		public HavingClause  Having  => SelectQuery.Having;
		public OrderByClause OrderBy => SelectQuery.OrderBy;

		public SelectQueryClause SelectQuery { get; private set; } = null!;

        public void SetSqlQuery(SelectQueryClause selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}

	public abstract class ClauseBase<T1> : SQLElement
		where T1 : ClauseBase<T1>
	{
		protected ClauseBase(SelectQueryClause? selectQuery, ClauseType clauseType, Type type) : base(clauseType, type)
        {
			SelectQuery = selectQuery!;
		}

		public SelectClause  Select  => SelectQuery.Select;
		public FromClause    From    => SelectQuery.From;
		public GroupByClause GroupBy => SelectQuery.GroupBy;
		public HavingClause  Having  => SelectQuery.Having;
		public OrderByClause OrderBy => SelectQuery.OrderBy;

		protected internal SelectQueryClause SelectQuery { get; private set; } = null!;

		internal void SetSqlQuery(SelectQueryClause selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}
}
