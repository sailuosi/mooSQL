#if !NET451
using Kdbndp;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 金仓数据字典：PG 兼容模式沿用 information_schema / pg_*。
    /// </summary>
    public class KingBaseSentence : NpgSentence
    {
        /// <summary>
        /// KingBaseSentence type.
        /// </summary>
        /// <summary>
        /// KingBaseSentence type.
        /// </summary>
        public KingBaseSentence(Dialect dialect) : base(dialect)
        {
        }
/// <summary>
/// Builds a connection string targeting the specified database.
/// </summary>
 

        protected override string BuildConnectionStringForDatabase(string dbName)
        {
            var builder = new KdbndpConnectionStringBuilder(DBLive.config.DBConnectStr)
            {
                Database = dbName
            };
            return builder.ConnectionString;
        }
/// <summary>
/// Creates a side connection for DDL/admin operations.
/// </summary>
 

        protected override DbConnection CreateSideConnection(string connectionString)
        {
            return new KdbndpConnection(connectionString);
        }
    }
}
#endif
