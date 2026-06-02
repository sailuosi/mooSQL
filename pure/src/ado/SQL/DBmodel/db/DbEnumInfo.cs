using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 DbEnumInfo。
    /// </summary>
    public class DbEnumInfo
    {

        /// <summary>
        /// 枚举类型标识
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 枚举项
        /// </summary>
        public Dictionary<string, string> Labels { get; set; }
    }
}