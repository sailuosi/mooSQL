using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// CockroachDB 方言：PostgreSQL wire + Npgsql；Bulk 使用 Fallback（避免 COPY BINARY 兼容坑）。
    /// </summary>
    public class CockroachDBDialect : NpgsqlDialect
    {
        /// <summary>
        /// CockroachDB 方言。
        /// </summary>
        public CockroachDBDialect() : base()
        {
            // CRDB 对部分 PG 高级特性有差异；保守关闭 Arrow 类假设由映射层处理
            Option.ProviderFlags.IsInsertReturningSupported = true;
        }

        /// <inheritdoc/>
        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);
    }
}
