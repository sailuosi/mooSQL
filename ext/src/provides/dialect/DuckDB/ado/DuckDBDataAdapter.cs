#if NET6_0_OR_GREATER
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB.NET 无内置 DataAdapter；用基类 <see cref="DbDataAdapter.Fill(System.Data.DataSet)"/> + SelectCommand 即可完成查询填充。
    /// </summary>
    public class DuckDBDataAdapter : DbDataAdapter
    {
        /// <summary>
        /// 初始化 <see cref="DuckDBDataAdapter"/> 实例。
        /// </summary>
        public DuckDBDataAdapter() { }

        /// <summary>
        /// 使用指定的 SelectCommand 初始化 <see cref="DuckDBDataAdapter"/>。
        /// </summary>
        /// <param name="selectCommand">用于填充的查询命令。</param>
        public DuckDBDataAdapter(DbCommand selectCommand)
        {
            SelectCommand = selectCommand;
        }
    }
}
#endif
