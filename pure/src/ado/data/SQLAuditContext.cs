using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 非查询执行成功后，用于 <see cref="MooEvents.onSQLRuned"/> 的快照上下文（异步派发时避免复用可变 <see cref="SQLCmd"/>）。
    /// </summary>
    public sealed class SQLAuditContext
    {
        /// <summary>SQL 文本快照。</summary>
        public SQLCmd Sql { get; }
        /// <summary>影响行数；未知时为 -1。</summary>
        public int RowsAffected { get; }
        /// <summary>连接位索引。</summary>
        public int DbPosition { get; }
        /// <summary>参数快照（浅拷贝）。</summary>
        public Paras Parameters { get; }
        /// <summary>所属数据库实例。</summary>
        public DBInstance Database { get; }

        public SQLAuditContext(SQLCmd sql, QueryType queryType, string targetTable, int rowsAffected, int dbPosition, Paras parameters, DBInstance database)
        {
            Sql = sql;
            RowsAffected = rowsAffected;
            DbPosition = dbPosition;
            Parameters = parameters ?? new Paras();
            Database = database;
        }
    }
}
