using System.Collections.Generic;
using System.Threading;
using mooSQL.data;
using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
    /// <summary>
    /// 对应于 SqlSourceBase
    /// </summary>
    public abstract class BaseSourceWord : ExpWordBase,ITableNode
	{
		protected BaseSourceWord(ClauseType clauseType, Type type = null) : base(clauseType, type)
        {
			SourceID = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
		}

		protected BaseSourceWord(int sourceId, ClauseType clauseType, Type type = null) : base(clauseType, type)
		{
			SourceID = sourceId;
		}

		public int SourceID { get; }

		public abstract SqlTableType          SqlTableType { get; }
		public abstract ITableNode       Source       { get; }
		public abstract FieldWord              All          { get; }
        public abstract string Name { get; }

        public abstract IList<IExpWord> GetKeys(bool allIfEmpty);
	}
}
