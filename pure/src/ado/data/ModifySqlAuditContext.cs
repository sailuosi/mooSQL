using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 非查询执行成功后，用于 <see cref="MooEvents.onModifySqlAudit"/> 的快照上下文（异步派发时避免复用可变 <see cref="SQLCmd"/>）。
    /// </summary>
    public sealed class ModifySqlAuditContext
    {
        /// <summary>SQL 文本快照。</summary>
        public string Sql { get; }
        /// <summary>语句类型（来自 <see cref="SQLCmd.type"/>）。</summary>
        public QueryType QueryType { get; }
        /// <summary>主目标表名。</summary>
        public string TargetTable { get; }
        /// <summary>影响行数；未知时为 -1。</summary>
        public int RowsAffected { get; }
        /// <summary>连接位索引。</summary>
        public int DbPosition { get; }
        /// <summary>参数快照（浅拷贝）。</summary>
        public Paras Parameters { get; }
        /// <summary>所属数据库实例。</summary>
        public DBInstance Database { get; }

        public ModifySqlAuditContext(string sql, QueryType queryType, string targetTable, int rowsAffected, int dbPosition, Paras parameters, DBInstance database)
        {
            Sql = sql ?? "";
            QueryType = queryType;
            TargetTable = targetTable ?? "";
            RowsAffected = rowsAffected;
            DbPosition = dbPosition;
            Parameters = parameters ?? new Paras();
            Database = database;
        }
    }
}
