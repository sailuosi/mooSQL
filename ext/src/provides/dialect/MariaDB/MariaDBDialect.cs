namespace mooSQL.data
{
    /// <summary>
    /// MariaDB：MySQL Family；Bulk 吃族默认 MySqlFamilyBulkCopyee；差分靠 ProviderFlags。
    /// </summary>
    public class MariaDBDialect : MySqlFamilyDialect
    {
        public MariaDBDialect()
        {
            // 保守默认：覆盖常见 10.11 / 11.x LTS（Arrow 需 13.1+）
            Option.ProviderFlags.IsJsonArrowSupported = false;
            Option.ProviderFlags.IsInsertReturningSupported = true;
            Option.ProviderFlags.IsJsonNativeBinary = false;
        }
    }
}
