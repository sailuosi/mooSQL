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
		/// <summary>
		/// 分配新的 <see cref="SourceID"/>。
		/// </summary>
		protected BaseSourceWord(ClauseType clauseType, Type type = null) : base(clauseType, type)
        {
			SourceID = Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
		}

		/// <summary>
		/// 使用已有来源 ID（用于子查询等复用）。
		/// </summary>
		protected BaseSourceWord(int sourceId, ClauseType clauseType, Type type = null) : base(clauseType, type)
		{
			SourceID = sourceId;
		}

		/// <summary>来源表在查询中的唯一编号。</summary>
		public int SourceID { get; }

		/// <inheritdoc />
		public abstract SqlTableType          SqlTableType { get; }
		/// <inheritdoc />
		public abstract ITableNode       Source       { get; }
		/// <inheritdoc />
		public abstract FieldWord              All          { get; }
        /// <inheritdoc />
        public abstract string Name { get; }

        /// <summary>
        /// 获取主键或索引列表达式；<paramref name="allIfEmpty"/> 控制是否回退为全列。
        /// </summary>
        public abstract IList<IExpWord> GetKeys(bool allIfEmpty);
	}
}
