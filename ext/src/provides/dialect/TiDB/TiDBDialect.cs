namespace mooSQL.data
{
    /// <summary>
    /// TiDB 方言：MySQL Family；ADO 为 MySqlConnector，Express 为薄封装。
    /// </summary>
    public class TiDBDialect : MySqlFamilyDialect
    {
        /// <summary>
        /// TiDBDialect type.
        /// </summary>
        /// <summary>
        /// TiDBDialect type.
        /// </summary>
        public TiDBDialect()
        {
            expression = new TiDBExpress(this);
        }
    }
}
