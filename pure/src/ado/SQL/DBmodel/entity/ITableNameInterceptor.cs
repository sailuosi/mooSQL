using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 表名解析器
    /// </summary>
    public interface ITableNameInterceptor
    {
        /// <summary>
        /// 执行表名解析
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        string Parse<T> (T value);

    }
}
