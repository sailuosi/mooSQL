using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Threading;



namespace mooSQL.data.model
{
	/// <summary>
	/// 表示来源表，约等于 TableWord+DerivedTable
	/// </summary>
	public class TableSourceWord : Clause,ITableNode
	{
#if DEBUG
		readonly int id = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
#endif
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTableSource(this);
        }
        public TableSourceWord(ITableNode source, string? alias)
			: this(source, alias, null)
		{
		}
		public TableSourceWord() : base(ClauseType.TableSource, null) { }
        public TableSourceWord(ITableNode source, string? alias, params JoinTableWord[]? joins) : base(ClauseType.TableSource, null)
        {
			Source = source ?? throw new ArgumentNullException(nameof(source));
			_alias = alias;

			if (joins != null)
				Joins.AddRange(joins);
		}

		public TableSourceWord(ITableNode source, string? alias, IEnumerable<JoinTableWord> joins, IEnumerable<IExpWord[]>? uniqueKeys) 
			: base(ClauseType.TableSource, null)
        {
			Source = source ?? throw new ArgumentNullException(nameof(source));
			_alias = alias;

			if (joins != null)
				Joins.AddRange(joins);

			if (uniqueKeys != null)
				UniqueKeys.AddRange(uniqueKeys);
		}

		public ITableNode Source { get; set; } = null;
		public SqlTableType    SqlTableType => Source.SqlTableType;

		private string? _alias;
		public  string?  Alias
		{
			get
			{
				if (string.IsNullOrEmpty(_alias))
				{
					if (Source is TableSourceWord sqlSource)
						return sqlSource.Alias;

					if (Source is TableWord sqlTable)
						return sqlTable.Alias;
				}

				return _alias;
			}
			set => _alias = value;
		}

		public string? RawAlias => _alias;

		private List<IExpWord[]>? _uniqueKeys;

		/// <summary>
		/// 主键列集合
		/// </summary>
		public  List<IExpWord[]>  UniqueKeys    => _uniqueKeys ??= new List<IExpWord[]>();

		public  bool                    HasUniqueKeys => _uniqueKeys != null && _uniqueKeys.Count > 0;

		public void Modify(ITableNode source)
		{
			Source = source;
		}


		public List<JoinTableWord> Joins { get; private set; } = new();



#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif




		public int       SourceID => Source.SourceID;
		public FieldWord  All      => Source.All;


        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            return Source.GetKeys(allIfEmpty);
        }



		public override ClauseType NodeType => ClauseType.TableSource;

        public string Name => throw new NotImplementedException();






		public void Deconstruct(out ITableNode source)
		{
			source = Source;
		}


    }
}
