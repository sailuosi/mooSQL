namespace mooSQL.data
{
    /// <summary>
    /// Amazon Aurora MySQL 兼容方言：复用 MySQL Family（MySqlConnector）。
    /// </summary>
    public class AuroraMySQLDialect : MySqlFamilyDialect
    {
        /// <summary>
        /// Aurora MySQL 方言。
        /// </summary>
        public AuroraMySQLDialect()
        {
            // Aurora MySQL 与社区 MySQL 8 对齐；RETURNING 仍按族默认关闭
            Option.ProviderFlags.IsJsonArrowSupported = true;
            Option.ProviderFlags.IsInsertReturningSupported = false;
            Option.ProviderFlags.IsJsonNativeBinary = true;
        }
    }
}
