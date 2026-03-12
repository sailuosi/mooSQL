// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 方言子组件接口，用于获取所属方言实例。
    /// </summary>
    public interface IDialectChild
    {
        /// <summary>所属方言实例。</summary>
        Dialect dialect { get; }
    }
}
