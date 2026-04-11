using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>MERGE 语句中 WHEN 子句的动作种类。</summary>
    public enum MergeOperateType
    {
        /// <summary>WHEN NOT MATCHED THEN INSERT。</summary>
        Insert,
        /// <summary>WHEN MATCHED THEN UPDATE。</summary>
        Update,
        /// <summary>WHEN MATCHED THEN DELETE。</summary>
        Delete,
        /// <summary>匹配时先 UPDATE 再按条件 DELETE。</summary>
        UpdateWithDelete,
        /// <summary>源侧未匹配时 UPDATE（BY SOURCE）。</summary>
        UpdateBySource,
        /// <summary>源侧未匹配时 DELETE（BY SOURCE）。</summary>
        DeleteBySource
    }
}
