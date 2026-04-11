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
        /// <summary>可选取消标记，用于长时间查询。</summary>
        public CancellationToken? cancellationToken;

        /// <summary>当前查询使用的数据库实例。</summary>
        public DBInstance DB;
    }
}
