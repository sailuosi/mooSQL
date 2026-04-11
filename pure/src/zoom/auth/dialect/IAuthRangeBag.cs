// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 权限范围集合
    /// </summary>
    public interface IAuthRangeBag
    {
        /// <summary>
        /// 执行编制
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        List<string> buildWhere(List<string> wh);
    }
}