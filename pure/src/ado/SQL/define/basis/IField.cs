using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 字段类型的约束，字段必须要有所属表。
    /// </summary>
    public interface IField
    {
        /// <summary>
        /// 所属表
        /// </summary>
        ITableNode BelongTable { get; }
    }
}
