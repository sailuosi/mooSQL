using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 导航属性
    /// </summary>
    public class EntityNavi
    {
        /// <summary>
        /// 主表的字段
        /// </summary>
        public string BossKey { get; set; }
        /// <summary>
        /// 主表字段2
        /// </summary>
        public string BossKey2 { get; set; }
        /// <summary>
        /// 映射类型
        /// </summary>
        public Type MappingType { get; set; }

        public Type ChildType { get; set; }
        /// <summary>
        /// 子表上关联主表的外键
        /// </summary>
        public string SlaveKey { get; set; }
        /// <summary>
        /// 子表上关联主表的外键2
        /// </summary>
        public string SlaveKey2 { get; set; }
        /// <summary>
        /// 导航类型
        /// </summary>
        public EnityNaviType NavigatType { get; set; }
        /// <summary>
        /// 条件
        /// </summary>
        public string WhereSql { get; set; }
    }
    /// <summary>
    /// 导航类型
    /// </summary>
    public enum EnityNaviType
    {
        /// <summary>
        /// 一对一
        /// </summary>
        OneToOne = 1,
        /// <summary>
        /// 一对多
        /// </summary>
        OneToMany = 2,
        /// <summary>
        /// 多对一
        /// </summary>
        ManyToOne = 3,
        /// <summary>
        /// 多对多
        /// </summary>
        ManyToMany = 4,
        /// <summary>
        /// 动态
        /// </summary>
        Dynamic = 5
    }
}
