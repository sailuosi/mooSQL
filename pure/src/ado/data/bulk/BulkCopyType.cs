using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 批复制类型
    /// </summary>
    public enum BulkCopyType
    {
        /// <summary>
        /// LINQ To DB will select copy method based on current provider.
        /// Default method usually set at [PROVIDER_NAME_HERE]Tools.DefaultBulkCopyType.
        /// </summary>
        Default = 0,
        /// <summary>
        /// 按序逐行插入
        /// </summary>
        RowByRow,
        /// <summary>
        /// 批量复制，不支持时回退到逐行
        /// </summary>
        MultipleRows,
        /// <summary>
        /// 驱动层支持。
        /// </summary>
        ProviderSpecific
    }
}
