using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace mooSQL.data.model
{

	/// <summary>
	/// select表达式
	/// </summary>
	[DebuggerDisplay("SQL = {" + nameof(SqlText) + "}")]
	public class SelectQueryClause : ExpWordBase,ITableNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
			return visitor.VisitSelectQuery(this);
        }
        #region Init

        /// <summary>
        /// 构造完整 SELECT 查询树并分配新的 <see cref="SourceID"/>。
        /// </summary>
        public SelectQueryClause( Type type = null) : base(ClauseType.SelectStatement, type)
        {
			SourceID = Interlocked.Increment(ref SourceIDCounter);

			Select  = new(this);
			From    = new(this);
			Where   = new(this);
			GroupBy = new(this);
			Having  = new(this);
			OrderBy = new(this);
		}

		/// <summary>
		/// 使用已有来源 ID 构造（子查询复用等场景）。
		/// </summary>
        public SelectQueryClause(int id,Type type = null) : base(ClauseType.SelectStatement, type)
		{
			SourceID = id;
		}

		/// <summary>
		/// 用各子句与扩展信息初始化查询树（克隆/反序列化路径）。
		/// </summary>
		public void Init(SelectClause select,
			FromClause                  from,
			WhereClause                 where,
			GroupByClause               groupBy,
			HavingClause                having,
			OrderByClause               orderBy,
			List<SetOperatorWord>?          setOperators,
			List<IExpWord[]>?        uniqueKeys,
			bool                           parameterDependent,
			string?                        queryName,
			bool                           doNotSetAliases)
		{
			Select               = select;
			From                 = from;
			Where                = where;
			GroupBy              = groupBy;
			Having               = having;
			OrderBy              = orderBy;
			_setOperators        = setOperators;
			IsParameterDependent = parameterDependent;
			QueryName            = queryName;
			DoNotSetAliases      = doNotSetAliases;

			if (uniqueKeys != null)
				UniqueKeys.AddRange(uniqueKeys);

			foreach (var col in select.Columns.content)
				col.Parent = this;

			Select. SetSqlQuery(this);
			From.   SetSqlQuery(this);
			Where.  SetSqlQuery(this);
			GroupBy.SetSqlQuery(this);
			Having. SetSqlQuery(this);
			OrderBy.SetSqlQuery(this);
		}

		/// <summary>SELECT 列表与 DISTINCT/TAKE/SKIP。</summary>
		public SelectClause  Select  { get;  set; } = null!;
		/// <summary>FROM 与表连接。</summary>
		public FromClause    From    { get;  set; } = null!;
		/// <summary>WHERE 条件。</summary>
		public WhereClause   Where   { get;  set; } = null!;
		/// <summary>GROUP BY。</summary>
		public GroupByClause GroupBy { get;  set; } = null!;
		/// <summary>HAVING。</summary>
		public HavingClause  Having  { get;  set; } = null!;
		/// <summary>ORDER BY。</summary>
		public OrderByClause OrderBy { get;  set; } = null!;

		private List<object>? _properties;
		/// <summary>附加属性袋（优化器/方言扩展等）。</summary>
		public  List<object>   Properties => _properties ??= new ();



		/// <summary>是否包含 SKIP 或 TAKE（受限结果集）。</summary>
		public bool           IsLimited        => Select.SkipValue != null || Select.TakeValue != null;
		/// <summary>查询是否依赖参数（行值表等）。</summary>
		public bool           IsParameterDependent { get; set; }

		/// <summary>
		/// Gets or sets flag when sub-query can be removed during optimization.
		/// </summary>
		public bool                     DoNotRemove        { get; set; }
		/// <summary>调试或注释中的查询名称。</summary>
		public string?                  QueryName          { get; set; }
		/// <summary>方言/扩展查询附加片段。</summary>
		public List<QueryExtension>? SqlQueryExtensions { get; set; }
		/// <summary>为 true 时不自动设置列别名。</summary>
		public bool                     DoNotSetAliases    { get; set; }

		List<IExpWord[]>? _uniqueKeys;

		/// <summary>
		/// Contains list of columns that build unique key for this sub-query.
		/// Used in JoinOptimizer for safely removing sub-query from resulting SQL.
		/// </summary>
		public List<IExpWord[]> UniqueKeys
		{
			get => _uniqueKeys ??= new();
			internal set => _uniqueKeys = value;
		}

		/// <summary>是否已记录唯一键列（用于连接优化）。</summary>
		public  bool                   HasUniqueKeys => _uniqueKeys?.Count > 0;

		#endregion



		private List<SetOperatorWord>? _setOperators;
		/// <summary>UNION / EXCEPT / INTERSECT 等集合运算子句。</summary>
		public  List<SetOperatorWord>  SetOperators
		{
			get => _setOperators ??= new List<SetOperatorWord>();
			internal set => _setOperators = value;
		}

		/// <summary>是否包含集合运算。</summary>
		public bool HasSetOperators => _setOperators != null && _setOperators.Count > 0;

		/// <summary>追加 UNION 或 UNION ALL。</summary>
		public void AddUnion(SelectQueryClause union, bool isAll)
		{
			SetOperators.Add(new SetOperatorWord(union, isAll ? SetOperation.UnionAll : SetOperation.Union));
		}





		/// <summary>全局递增的表/子查询来源编号计数器。</summary>
		public static int SourceIDCounter;

		/// <summary>本查询在父查询中的来源编号。</summary>
		public int           SourceID { get; }
		/// <inheritdoc />
		public SqlTableType  SqlTableType => SqlTableType.Table;


		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SqlQuery;



		/// <inheritdoc />
		public override int Precedence => PrecedenceLv.Unknown;

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		/// <summary>在 FROM 中解析 <paramref name="table"/> 对应的来源节点。</summary>
        public ITableNode? GetTableSource(ITableNode table)
        {
            foreach(var t in From.Tables) { 
				
				if(t == table) return t;
			}

            return null;
        }
        /// <inheritdoc />
        public override Type? SystemType
        {
            get
            {
                if (Select.Columns.Count == 1)
                    return Select.Columns[0].SystemType;

                //if (From.Tables.Count == 1 && From.Tables[0] is TableSourceWord src && src.Joins.Count == 0)
                    //return src.SystemType;

                return null;
            }
        }
        /// <inheritdoc />
        public IElementWriter ToString(IElementWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			//writer.DebugAppendUniqueId(this);

			writer
				.Append('(')
				.Append(SourceID)
				.Append(')');

			if (DoNotRemove)
				writer.Append("DNR");

			writer.Append(' ');

			if (QueryName != null)
				writer.Append("/* "+QueryName+" */ ");

			writer
				.AppendElement(Select)
				.AppendElement(From)
				.AppendElement(Where)
				.AppendElement(GroupBy)
				.AppendElement(Having)
				.AppendElement(OrderBy);

			if (HasSetOperators)
				foreach (ISQLNode u in SetOperators)
					writer.AppendElement(u);

			//writer.AppendExtensions(SqlQueryExtensions);

			writer.RemoveVisited(this);

			return writer;
		}



		/// <summary>调试用的完整 SQL 文本。</summary>
		public string SqlText => this.ToDebugString(this);

		/// <inheritdoc />
        public string Name => throw new NotImplementedException();

        private FieldWord? _all;
		/// <inheritdoc />
        public FieldWord All
        {
            get => _all ??= FieldWord.All(this);

            internal set
            {
                _all = value;

                if (_all != null)
                    _all.Table = this;
            }
        }
        /// <inheritdoc />
        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            if (Select.Columns.Count > 0 && From.Tables.Count == 1 && From.Tables[0] is TableSourceWord src && src.Joins.Count == 0)
            {
                var tableKeys = src.GetKeys(allIfEmpty);

                return tableKeys;
            }

            return null;
        }

		/// <summary>递归清空各子句状态。</summary>
        public void Cleanup()
		{
			Select.Cleanup();
			From.Cleanup();
			Where.Cleanup();
			GroupBy.Cleanup();
			Having.Cleanup();
			OrderBy.Cleanup();
		}


    }
}
