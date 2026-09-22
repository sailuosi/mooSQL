#if NET6_0_OR_GREATER
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB.NET 无内置 DataAdapter；用基类 <see cref="DbDataAdapter.Fill"/> + SelectCommand 即可完成查询填充。
    /// </summary>
    public class DuckDBDataAdapter : DbDataAdapter
    {
        public DuckDBDataAdapter() { }

        public DuckDBDataAdapter(DbCommand selectCommand)
        {
            SelectCommand = selectCommand;
        }
    }
}
#endif
