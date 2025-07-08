// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 包含下级的
    /// </summary>
    public interface Childable
    {
        /// <summary>
        /// 是否是子项
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        bool isChildOf(Childable a);
        /// <summary>
        /// 是否相同
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        bool isSame(Childable a);
    }


    /// <summary>
    /// 可检查相同的
    /// </summary>
    public interface Samable
    {
        /// <summary>
        /// 是否相同
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        bool isSame(Samable a);
    }
}