using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>描述正在由 <see cref="SQLBuilder"/> 生成的 SQL 片段及其目标语句类型。</summary>
    public class BuildingSQL
    {
        /// <summary>
        /// 来源的语句类型
        /// </summary>
        public ClauseType SrcClauseType { get; set; }


        /// <summary>关联的链式 SQL 构造器。</summary>
        public SQLBuilder Builder { get; set; }
        /// <summary>
        /// 将当前构建状态物化为 <see cref="SQLCmd"/>（Select/Insert/Update 等差异由此委托承担，
        /// 不在轻量的 <see cref="SQLBuilder"/> 上挂命令缓存）。
        /// </summary>
        public Func<BuildingSQL, SQLCmd> ToCmd { get; set; }
        /// <summary>
        /// SQL的类型
        /// </summary>
        public SentenceType SQLType { get; set; }
    }
}
