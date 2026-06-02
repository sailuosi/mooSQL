using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 DbTypeInfo。
    /// </summary>
    public class DbTypeInfo
    {

        /// <summary>
        /// 类型标识
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 枚举项
        /// </summary>
        public List<LabelInfo> Labels { get; set; }

        /// <summary>
        /// 类型 LabelInfo。
        /// </summary>
        public class LabelInfo
        {
            /// <summary>
            /// 属性 label（string）。
            /// </summary>
            public string label { get; }
            /// <summary>
            /// 属性 value（string）。
            /// </summary>
            public string value { get; }

            /// <summary>
            /// 初始化 LabelInfo（构造）。
            /// </summary>
            public LabelInfo(string label, string value)
            {
                this.label = label;
                this.value = value;
            }
        }
    }
}