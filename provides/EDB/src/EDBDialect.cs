using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// EnterpriseDB (EDB) 方言：PostgreSQL 兼容，复用 Npgsql；Bulk 使用 Fallback。
    /// </summary>
    public class EDBDialect : NpgsqlDialect
    {
        /// <summary>
        /// EDB 方言。
        /// </summary>
        public EDBDialect() : base()
        {
            Option.ProviderFlags.IsInsertReturningSupported = true;
        }

        /// <inheritdoc/>
        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);
    }
}
