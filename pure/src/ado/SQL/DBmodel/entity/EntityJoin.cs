using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 实体连接信息，用于定义两个表之间的关联关系。
    /// </summary>
    public class EntityJoin
    {
        /// <summary>
        /// 连接类型
        /// </summary>
        public JoinKind Type { get; set; }
        /// <summary>
        /// 连接的表名
        /// </summary>
        public string To { get; set; }
        /// <summary>
        /// 连接的别名
        /// </summary>
        public string As { get; set; }
        /// <summary>
        /// 连接的条件，定义时，为完整的join on条件。不再需要OnA和OnB字段。
        /// </summary>
        public string On { get; set; }
        /// <summary>
        /// 连接的条件，定义时，为on的左侧字段。
        /// </summary>
        public string OnA { get; set; }
        /// <summary>
        /// 连接的条件，定义时，为on的右侧字段。
        /// </summary>
        public string OnB { get; set; }
    }
}
