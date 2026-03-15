using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    internal enum ExecuteType
    {
        /// <summary>
        /// 未知类型
        /// </summary>
        none=0,
        /// <summary>
        /// 修改类操作，执行时调用 nonquery方法
        /// </summary>
        modify=1,
        /// <summary>
        /// 查询类操作，执行时调用 query
        /// </summary>
        query =2
    }
}
