#if NET10_0_OR_GREATER
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// SonnetDB.Data 无内置 DataAdapter；用基类 <see cref="DbDataAdapter.Fill"/> + SelectCommand 即可完成查询填充。
    /// </summary>
    public class SonnetDBDataAdapter : DbDataAdapter
    {
        public SonnetDBDataAdapter() { }

        public SonnetDBDataAdapter(DbCommand selectCommand)
        {
            SelectCommand = selectCommand;
        }
    }
}
#endif
