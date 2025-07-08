using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class ClipTable
    {
        /// <summary>
        /// 绑定值
        /// </summary>
        public object BindValue;
        /// <summary>
        /// 表的类型
        /// </summary>
        public Type EnityType;
        /// <summary>
        /// 表的数据库映射信息
        /// </summary>
        public EntityInfo TableInfo;
        /// <summary>
        /// 表的别名
        /// </summary>
        public string Alias;
        /// <summary>
        /// 绑定方式
        /// </summary>
        public ClipTableType BType;
    }

    internal enum ClipTableType { 
        None=0,
        FromBy=1,
        JoinBy=2,
        UpdateBy=3,
        DeleteBy=4
    }
}
