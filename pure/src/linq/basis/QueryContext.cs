using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 查询上下文
    /// </summary>
    public class QueryContext
    {
        public CancellationToken? cancellationToken;

        public DBInstance DB;
    }
}
