using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 代表类的继承关系，用以支持实体类的继承。
    /// </summary>
    public class EntiyInherit
    {
        /// <summary>
        /// 继承的描述值
        /// </summary>
        public object? Code;
        /// <summary>
        /// 是否默认映射
        /// </summary>
        public bool IsDefault;
        /// <summary>
        /// 关联类
        /// </summary>
        public Type Type = null!;
        /// <summary>
        /// 列描述
        /// </summary>
        public EntityInfo Entity = null!;
    }
}
