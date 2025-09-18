using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class EntityOrder
    {
        /// <summary>
        /// 排序序号
        /// </summary>
        public int Idx { get; set; }
        /// <summary>
        /// 来源表名的简称
        /// </summary>
        public string Nick { get; set; }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string Field { get; set; }
        /// <summary>
        /// 排序类型
        /// </summary>
        public OrderType OType { get; set; }
    }
}
