using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 开启SQL子条件范围的前缀
    /// </summary>
    public enum SinkType
    {
        /// <summary>
        /// 无前缀，即不开启子条件范围
        /// </summary>
        None=0,
        /// <summary>
        /// 开启子条件范围，前缀为AND
        /// </summary>
        And=1,
        /// <summary>
        /// 开启子条件范围，前缀为OR
        /// </summary>
        Or=2,
        /// <summary>
        /// 
        /// </summary>
        AndNot=3,
        /// <summary>
        /// 开启子条件范围，前缀为OR NOT
        /// </summary>
        OrNot=4,
    }
}
