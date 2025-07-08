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
        public override Clause Accept(ClauseVisitor visitor)
        {
			return visitor.VisitSelectQuery(this);
        }
        #region Init

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

        public SelectQueryClause(int id,Type type = null) : base(ClauseType.SelectStatement, type)
		{
			SourceID = id;
		}

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

		public SelectClause  Select  { get;  set; } = null!;
		public FromClause    From    { get;  set; } = null!;
		public WhereClause   Where   { get;  set; } = null!;
		public GroupByClause GroupBy { get;  set; } = null!;
		public HavingClause  Having  { get;  set; } = null!;
		public OrderByClause OrderBy { get;  set; } = null!;

		private List<object>? _properties;
		public  List<object>   Properties => _properties ??= new ();



		public bool           IsLimited        => Select.SkipValue != null || Select.TakeValue != null;
		public bool           IsParameterDependent { get; set; }

		/// <summary>
		/// Gets or sets flag when sub-query can be removed during optimization.
		/// </summary>
		public bool                     DoNotRemove        { get; set; }
		public string?                  QueryName          { get; set; }
		public List<QueryExtension>? SqlQueryExtensions { get; set; }
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

		public  bool                   HasUniqueKeys => _uniqueKeys?.Count > 0;

		#endregion



		private List<SetOperatorWord>? _setOperators;
		public  List<SetOperatorWord>  SetOperators
		{
			get => _setOperators ??= new List<SetOperatorWord>();
			internal set => _setOperators = value;
		}

		public bool HasSetOperators => _setOperators != null && _setOperators.Count > 0;

		public void AddUnion(SelectQueryClause union, bool isAll)
		{
			SetOperators.Add(new SetOperatorWord(union, isAll ? SetOperation.UnionAll : SetOperation.Union));
		}





		public static int SourceIDCounter;

		public int           SourceID { get; }
		public SqlTableType  SqlTableType => SqlTableType.Table;


		public override ClauseType NodeType => ClauseType.SqlQuery;



		public override int Precedence => PrecedenceLv.Unknown;

		public override bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

        public ITableNode? GetTableSource(ITableNode table)
        {
            foreach(var t in From.Tables) { 
				
				if(t == table) return t;
			}

            return null;
        }
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



		public string SqlText => this.ToDebugString(this);

        public string Name => throw new NotImplementedException();

        private FieldWord? _all;
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
        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            if (Select.Columns.Count > 0 && From.Tables.Count == 1 && From.Tables[0] is TableSourceWord src && src.Joins.Count == 0)
            {
                var tableKeys = src.GetKeys(allIfEmpty);

                return tableKeys;
            }

            return null;
        }

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
