using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 批量复制时源列与目标列的单条映射。
    /// </summary>
    public class DbBulkFieldMap
    {
        /// <summary>映射方式（按索引或按名称）。</summary>
        public BulkMapType type;
        /// <summary>源列索引（按索引映射时使用）。</summary>
        public int srcIndex=-1;
        /// <summary>目标列索引（按索引映射时使用）。</summary>
        public int tarIndex=-1;

        /// <summary>源列名（按名称映射时使用）。</summary>
        public string srcName;
        /// <summary>目标列名（按名称映射时使用）。</summary>
        public string tarName;
    }

    /// <summary>
    /// 批量复制列映射类型。
    /// </summary>
    public enum BulkMapType
    {
        /// <summary>无映射。</summary>
        none=0,
        /// <summary>按列索引映射。</summary>
        index=1,
        /// <summary>按列名映射。</summary>
        name=2
    }
}
