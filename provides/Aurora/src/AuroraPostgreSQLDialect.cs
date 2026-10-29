using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// Amazon Aurora PostgreSQL 兼容方言：复用 Npgsql；Bulk 退化为多行 INSERT（云托管 COPY 权限差异）。
    /// </summary>
    public class AuroraPostgreSQLDialect : NpgsqlDialect
    {
        /// <summary>
        /// Aurora PostgreSQL 方言。
        /// </summary>
        public AuroraPostgreSQLDialect() : base()
        {
        }

        /// <inheritdoc/>
        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);
    }
}
