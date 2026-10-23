#if NET8_0_OR_GREATER
using HuaweiCloud.GaussDB;
#else
using Npgsql;
#endif
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// openGauss / GaussDB 数据字典：PG 兼容模式沿用 information_schema / pg_*。
    /// </summary>
    public class OpenGaussSentence : NpgSentence
    {
        /// <summary>
        /// OpenGaussSentence type.
        /// </summary>
        /// <summary>
        /// OpenGaussSentence type.
        /// </summary>
        public OpenGaussSentence(Dialect dialect) : base(dialect)
        {
        }

#if NET8_0_OR_GREATER
 /// <summary>
 /// Builds a connection string targeting the specified database.
 /// </summary>
 
        protected override string BuildConnectionStringForDatabase(string dbName)
        {
            var builder = new GaussDBConnectionStringBuilder(DBLive.config.DBConnectStr)
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
            return new GaussDBConnection(connectionString);
        }
#endif
    }
}
