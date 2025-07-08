using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 字段种类，用于区分字段的来源，比如是基本表中的还是连接表的。
    /// </summary>
    public enum FieldKind
    {
        /// <summary>
        /// 未定义。
        /// </summary>
        None=0,
        /// <summary>
        /// 基本表中的字段。
        /// </summary>
        Base=1,
        /// <summary>
        /// 连接表中的字段。
        /// </summary>
        Join=2,
        /// <summary>
        /// 自由字段，比如聚合函数之类的。
        /// </summary>
        Free=3,
        /// <summary>
        /// 纯虚字段。不需要出现在SQL语句中。
        /// </summary>
        Fake=9
    }
}
