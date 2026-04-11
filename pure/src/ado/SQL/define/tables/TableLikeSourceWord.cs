using System;
using System.Collections.Generic;
using mooSQL.data;
using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	/// <summary>
	/// 表值来源：可由 VALUES、子查询等构成，行为类似表。
	/// </summary>
	public class TableLikeSourceWord : BaseSourceWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTableLikeSource(this);
        }
        /// <summary>
        /// 构造空的表状来源（类型可选）。
        /// </summary>
        public TableLikeSourceWord(Type type=null):base(ClauseType.SqlTableLikeSource,type)
		{
		}

        /// <summary>
        /// 使用显式来源 ID、行集或子查询及字段列表构造。
        /// </summary>
        public TableLikeSourceWord(
			int                   id,
			ValuesTableWord?       sourceEnumerable,
			SelectQueryClause?          sourceQuery,
			IEnumerable<FieldWord> sourceFields, Type type = null) : base(id,ClauseType.SqlTableLikeSource, type)
		{
			SourceEnumerable = sourceEnumerable;
			SourceQuery      = sourceQuery;

			foreach (var field in sourceFields)
				AddField(field);
		}


		/// <summary>展开后的列字段列表。</summary>
		public List<FieldWord> SourceFields { get; } = new ();

		void AddField(FieldWord field)
		{
			field.Table = this;
			SourceFields.Add(field);
		}

		/// <summary>行值表来源（若有）。</summary>
		public ValuesTableWord?  SourceEnumerable { get; set; }
		/// <summary>子查询来源（若有）。</summary>
		public SelectQueryClause?     SourceQuery      { get; set; }
		/// <inheritdoc />
		public override ITableNode  Source => (ITableNode?)SourceQuery ?? SourceEnumerable!;

		/// <summary>是否依赖参数（行集来源恒为 true）。</summary>
		public bool IsParameterDependent
		{
			// enumerable source allways parameter-dependent
			get => SourceQuery?.IsParameterDependent ?? true;
			set
			{
				if (SourceQuery != null)
					SourceQuery.IsParameterDependent = value;
			}
		}



		/// <inheritdoc />
		public override ClauseType       NodeType => ClauseType.SqlTableLikeSource;

        /// <inheritdoc />
        public override Type SystemType => throw new NotImplementedException();
        /// <inheritdoc />
        public override SqlTableType SqlTableType => SqlTableType.MergeSource;

		FieldWord?                _all;
		/// <inheritdoc />
		public override FieldWord All => _all ??= FieldWord.All(this);

        /// <inheritdoc />
        public override string Name => throw new NotImplementedException();

        /// <inheritdoc />
        public override int Precedence => throw new NotImplementedException();

        /// <inheritdoc />
        public override IList<IExpWord> GetKeys(bool allIfEmpty) => throw new NotImplementedException();

		/// <inheritdoc />
		public override bool Equals(IExpWord? other) => throw new NotImplementedException();

        /// <inheritdoc />
        public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            throw new NotImplementedException();
        }
    }
}
