using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 仓储的查询动作类型
    /// </summary>
    public enum QueryAction
    {
        /// <summary>
        /// 未知
        /// </summary>
        None=0,
        /// <summary>
        /// 查询列表，包含多个和翻页
        /// </summary>
        QueryList=1,
        /// <summary>
        /// 查询一个
        /// </summary>
        QueryOne=2,
        /// <summary>
        /// 更新
        /// </summary>
        Update= 3,
        /// <summary>
        /// 删除
        /// </summary>
        Delete=4,
    }
}
