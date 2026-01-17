using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 
    /// </summary>
    public enum CleanWay
    {
        /// <summary>
        /// 在修改后清理
        /// </summary>
        AfterModify =0,
        /// <summary>
        /// 总是清理
        /// </summary>
        Always=1,
        /// <summary>
        /// 从不清理
        /// </summary>
        Never=2,
    }
}
