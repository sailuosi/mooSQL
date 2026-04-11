using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Threading;



namespace mooSQL.data.model
{
	/// <summary>
	/// FROM 子句中的表来源：包装基表/子查询等，并可带别名与一系列 <see cref="JoinTableWord"/>。
	/// </summary>
	public class TableSourceWord : Clause,ITableNode
	{
#if DEBUG
		readonly int id = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
#endif
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTableSource(this);
        }
        /// <summary>使用来源与别名，无额外 JOIN。</summary>
        public TableSourceWord(ITableNode source, string? alias)
			: this(source, alias, null)
		{
		}
		/// <summary>构造仅占位用的空表来源节点。</summary>
		public TableSourceWord() : base(ClauseType.TableSource, null) { }
        /// <summary>来源、别名与可选的 JOIN 列表。</summary>
        public TableSourceWord(ITableNode source, string? alias, params JoinTableWord[]? joins) : base(ClauseType.TableSource, null)
        {
			Source = source ?? throw new ArgumentNullException(nameof(source));
			_alias = alias;

			if (joins != null)
				Joins.AddRange(joins);
		}

		/// <summary>来源、别名、JOIN 列表以及可选的唯一键列组（用于 MERGE/UPSERT 等）。</summary>
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

		/// <summary>底层表或嵌套来源（子查询、表源等）。</summary>
		public ITableNode Source { get; set; } = null;
		/// <inheritdoc />
		public SqlTableType    SqlTableType => Source.SqlTableType;

		private string? _alias;
		/// <summary>当前来源的别名；未显式设置时可从嵌套 <see cref="TableSourceWord"/> 或 <see cref="TableWord"/> 继承。</summary>
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

		/// <summary>显式设置的别名字符串（不经过继承解析）。</summary>
		public string? RawAlias => _alias;

		private List<IExpWord[]>? _uniqueKeys;

		/// <summary>
		/// 主键或唯一索引列组（每组为一行键的列表达式列表）。
		/// </summary>
		public  List<IExpWord[]>  UniqueKeys    => _uniqueKeys ??= new List<IExpWord[]>();

		/// <summary>是否已记录至少一组唯一键列。</summary>
		public  bool                    HasUniqueKeys => _uniqueKeys != null && _uniqueKeys.Count > 0;

		/// <summary>替换底层来源节点。</summary>
		public void Modify(ITableNode source)
		{
			Source = source;
		}


		/// <summary>追加在该来源上的 INNER/LEFT 等 JOIN 链。</summary>
		public List<JoinTableWord> Joins { get; private set; } = new();



#if OVERRIDETOSTRING

		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif




		/// <inheritdoc />
		public int       SourceID => Source.SourceID;
		/// <inheritdoc />
		public FieldWord  All      => Source.All;


		/// <inheritdoc />
        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            return Source.GetKeys(allIfEmpty);
        }



		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.TableSource;

		/// <inheritdoc />
        public string Name => throw new NotImplementedException();






		/// <summary>解构为底层来源。</summary>
		public void Deconstruct(out ITableNode source)
		{
			source = Source;
		}


    }
}
