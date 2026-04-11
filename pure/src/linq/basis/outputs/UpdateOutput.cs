using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// MERGE/OUTPUT 等场景下同时返回删除与插入行的快照。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    public class UpdateOutput<T>
    {
        /// <summary>被删除行（旧值）。</summary>
        public T Deleted { get; set; } = default!;
        /// <summary>新插入行（新值）。</summary>
        public T Inserted { get; set; } = default!;
    }
}
