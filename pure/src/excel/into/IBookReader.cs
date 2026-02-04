using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 标准的Excel读取器，接收一组配置，读取一个文件流，并输出读取结果。
    /// </summary>
    public interface IBookReader
    {
        /// <summary>
        /// 设置要读取的文件流
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        IBookReader useStream(Stream stream);

        IBookReader useScope(ReadScopeConfig config);

        /// <summary>
        /// 输出一个Excel信息的book文件
        /// </summary>
        /// <returns></returns>
        XWorkBook asBook();
    }
}
