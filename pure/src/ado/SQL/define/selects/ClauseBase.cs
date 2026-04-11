namespace mooSQL.data.model
{
	/// <summary>依附于 <see cref="SelectQueryClause"/> 的子句片段（FROM/WHERE 等快捷访问）。</summary>
	public abstract class ClauseBase : SQLElement
	{
		/// <summary>绑定所属查询体。</summary>
		protected ClauseBase(SelectQueryClause? selectQuery, ClauseType clauseType, Type type) : base(clauseType, type)
        {
			SelectQuery = selectQuery!;
		}

		/// <summary>SELECT 列表。</summary>
		public SelectClause  Select  => SelectQuery.Select;
		/// <summary>FROM 子句。</summary>
		public FromClause    From    => SelectQuery.From;
		/// <summary>WHERE 子句。</summary>
		public WhereClause   Where   => SelectQuery.Where;
		/// <summary>GROUP BY 子句。</summary>
		public GroupByClause GroupBy => SelectQuery.GroupBy;
		/// <summary>HAVING 子句。</summary>
		public HavingClause  Having  => SelectQuery.Having;
		/// <summary>ORDER BY 子句。</summary>
		public OrderByClause OrderBy => SelectQuery.OrderBy;

		/// <summary>所属查询语法树。</summary>
		public SelectQueryClause SelectQuery { get; private set; } = null!;

        /// <summary>替换所属查询体引用。</summary>
        public void SetSqlQuery(SelectQueryClause selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}

	/// <summary>用于自引用子句类型的 <see cref="ClauseBase"/> 变体。</summary>
	public abstract class ClauseBase<T1> : SQLElement
		where T1 : ClauseBase<T1>
	{
		/// <summary>绑定所属查询体。</summary>
		protected ClauseBase(SelectQueryClause? selectQuery, ClauseType clauseType, Type type) : base(clauseType, type)
        {
			SelectQuery = selectQuery!;
		}

		/// <summary>SELECT 列表。</summary>
		public SelectClause  Select  => SelectQuery.Select;
		/// <summary>FROM 子句。</summary>
		public FromClause    From    => SelectQuery.From;
		/// <summary>GROUP BY 子句。</summary>
		public GroupByClause GroupBy => SelectQuery.GroupBy;
		/// <summary>HAVING 子句。</summary>
		public HavingClause  Having  => SelectQuery.Having;
		/// <summary>ORDER BY 子句。</summary>
		public OrderByClause OrderBy => SelectQuery.OrderBy;

		/// <summary>所属查询语法树。</summary>
		protected internal SelectQueryClause SelectQuery { get; private set; } = null!;

		/// <summary>内部：替换查询体引用。</summary>
		internal void SetSqlQuery(SelectQueryClause selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}
}
