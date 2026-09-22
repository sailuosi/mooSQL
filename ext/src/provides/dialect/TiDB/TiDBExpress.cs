namespace mooSQL.data
{
    /// <summary>
    /// TiDB 表达式：SQL 与 MySQL 一致；Provider 指向 MySqlConnector。
    /// </summary>
    public class TiDBExpress : MySQLExpress
    {
        public TiDBExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "?";
            _selectAutoIncrement = "SELECT Last_Insert_Id()";
            _provideType = "MySqlConnector.MySqlConnectorFactory, MySqlConnector";
        }
    }
}
