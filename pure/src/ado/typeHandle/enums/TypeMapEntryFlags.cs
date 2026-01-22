using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 类型映射条目标志，用于控制类型映射的行为
    /// </summary>
    [Flags]
    internal enum TypeMapEntryFlags
    {
        /// <summary>
        /// 无标志
        /// </summary>
        None = 0,
        /// <summary>
        /// 设置类型
        /// </summary>
        SetType = 1 << 0,
        /// <summary>
        /// 使用 GetFieldValue 方法
        /// </summary>
        UseGetFieldValue = 1 << 1,
    }
}
