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
        /// <summary>构造来源表描述。</summary>
        public OriginTable() { }
        /// <summary>
        /// 依据的实体类类型，也可能是匿名类型
        /// </summary>
        public Type EntityType { get; set; }
        /// <summary>
        /// 别名
        /// </summary>
        public string NickName { get; set; }

        /// <summary>来源类别：实体表、子查询或原始 SQL。</summary>
        public OriginTbType OType { get; set; }

        /// <summary>
        /// 根据运行类型生成 FROM/UPDATE/DELETE 目标 SQL 片段。
        /// </summary>
        public abstract string build(DBInstance DB,LayerRunType type);

    }

    /// <summary>
    /// 查询来源表在 SQL 中的形态分类。
    /// </summary>
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
