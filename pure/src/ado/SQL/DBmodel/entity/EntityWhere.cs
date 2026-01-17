using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体查询条件封装类
    /// </summary>
    public class EntityWhere
    {
        /// <summary>
        /// 是否开启子条件范围，及其范围类型
        /// </summary>
        public SinkType? Sink { get; set; }
        /// <summary>
        /// 是否关闭子范围
        /// </summary>
        public bool? Rise { get; set; }
        /// <summary>
        /// 绑定字段名称，用于动态绑定字段名时使用
        /// </summary>
        public string Bind {  get; set; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string Field { get; set; }
        /// <summary>
        /// 操作符，默认为等于（=）
        /// </summary>
        public string Op { get; set; }
        /// <summary>
        /// 字段值，可以是任意类型
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// 是否为函数值，默认为否
        /// </summary>
        public bool? IsFuncVal { get; set; }
        /// <summary>
        /// 是否为自定义条件，默认为否
        /// </summary>
        public bool? IsFree { get; set; }
        /// <summary>
        /// 自定义条件文本
        /// </summary>
        public Func<object> OnLoadValue { get; set; }
        /// <summary>
        /// 自定义条件构建方法
        /// </summary>
        public Func<SQLBuilder,string> OnBuildFree { get; set; }
    }
}
