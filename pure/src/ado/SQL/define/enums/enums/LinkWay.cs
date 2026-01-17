using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 表关联方式
    /// </summary>
    public enum LinkWay
    {
        None=0,
        /// <summary>
        /// 一对一关系
        /// </summary>
        OneToOne = 1,
        /// <summary>
        /// 一对多关系
        /// </summary>
        OneToMany=2,
        /// <summary>
        /// 多对一关系
        /// </summary>
        ManyToOne=3,
        /// <summary>
        /// 多对多关系
        /// </summary>
        ManyToMany=4,
    }
}
