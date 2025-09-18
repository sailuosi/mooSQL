using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库版本信息
    /// </summary>
    public class DBVersion
    {
        /// <summary>
        /// 版本号，为关键字
        /// </summary>
        public string VersionCode { get; set; }
        /// <summary>
        /// 版本名称
        /// </summary>
        public string VersionName { get; set; }
        /// <summary>
        /// 匹配正则表达式，用于判断版本号是否符合要求
        /// </summary>
        public string MatchRegex { get; set; }
        /// <summary>
        /// 版本号，为数字，用于比较版本号大小
        /// </summary>
        public double VersionNumber { get; set; }
        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime ReleaseTime { get; set; }
        /// <summary>
        /// 索引，用于排序
        /// </summary>
        public int Idx { get; set; }
        /// <summary>
        /// 发行年份
        /// </summary>
        public int Year { get; set; }
        /// <summary>
        /// 特性说明
        /// </summary>
        public string Note { get; set; }
    }
}
