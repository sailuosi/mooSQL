using System;
using System.Collections.Generic;
using mooSQL.data;
using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	public class TableLikeSourceWord : BaseSourceWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTableLikeSource(this);
        }
        public TableLikeSourceWord(Type type=null):base(ClauseType.SqlTableLikeSource,type)
		{
		}

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


		public List<FieldWord> SourceFields { get; } = new ();

		void AddField(FieldWord field)
		{
			field.Table = this;
			SourceFields.Add(field);
		}

		public ValuesTableWord?  SourceEnumerable { get; set; }
		public SelectQueryClause?     SourceQuery      { get; set; }
		public override ITableNode  Source => (ITableNode?)SourceQuery ?? SourceEnumerable!;

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



		public override ClauseType       NodeType => ClauseType.SqlTableLikeSource;

        public override Type SystemType => throw new NotImplementedException();
        public override SqlTableType SqlTableType => SqlTableType.MergeSource;

		FieldWord?                _all;
		public override FieldWord All => _all ??= FieldWord.All(this);

        public override string Name => throw new NotImplementedException();

        public override int Precedence => throw new NotImplementedException();

        public override IList<IExpWord> GetKeys(bool allIfEmpty) => throw new NotImplementedException();

		public override bool Equals(IExpWord? other) => throw new NotImplementedException();

        public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
        {
            throw new NotImplementedException();
        }
    }
}
