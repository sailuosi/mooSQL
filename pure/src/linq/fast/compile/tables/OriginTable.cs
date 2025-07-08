using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 一个查询的来源表
    /// </summary>
    public abstract class OriginTable
    {
        public OriginTable() { }
        /// <summary>
        /// 依据的实体类类型，也可能是匿名类型
        /// </summary>
        public Type EntityType { get; set; }
        /// <summary>
        /// 别名
        /// </summary>
        public string NickName { get; set; }

        public OriginTbType OType { get; set; }

        public abstract string build(DBInstance DB,LayerRunType type);

    }

    public enum OriginTbType { 
        /// <summary>
        /// 依据实体类
        /// </summary>
        Entity=0,
        /// <summary>
        /// 子查询
        /// </summary>
        SubQuery=1,
        /// <summary>
        /// 自定义的SQL字符串
        /// </summary>
        TextSQL=2,
    }
}
