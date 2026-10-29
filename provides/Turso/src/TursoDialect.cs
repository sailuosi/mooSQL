namespace mooSQL.data
{
    /// <summary>
    /// Turso / libSQL 方言：SQLite 协议兼容，复用 <see cref="SQLiteDialect"/>。
    /// 远程 libSQL 连接串由应用配置；本地嵌入式可继续用 Microsoft.Data.Sqlite 路径。
    /// </summary>
    public class TursoDialect : SQLiteDialect
    {
        /// <summary>
        /// Turso 方言。
        /// </summary>
        public TursoDialect() : base()
        {
            Option.ProviderFlags.IsTakeSupported = true;
            Option.ProviderFlags.IsSkipSupported = true;
            Option.ProviderFlags.IsSkipSupportedIfTake = true;
        }
    }
}
