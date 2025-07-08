using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    public class BuildingSQL
    {
        /// <summary>
        /// 来源的语句类型
        /// </summary>
        public ClauseType SrcClauseType { get; set; }


        public SQLBuilder Builder { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Func<BuildingSQL, SQLCmd> ToCmd { get; set; }
        /// <summary>
        /// SQL的类型
        /// </summary>
        public SentenceType SQLType { get; set; }
    }
}
