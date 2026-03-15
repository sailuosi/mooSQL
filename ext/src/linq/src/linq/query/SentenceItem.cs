using mooSQL.data.model;
using mooSQL.linq.Data;
using mooSQL.linq.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
    /// <summary>
    /// 代表着一个SQL定义模型
    /// </summary>
    internal class SentenceItem
    {


        public BaseSentence Statement { get; set; } = null!;
        public object? Context { get; set; }
        public bool IsContinuousRun { get; set; }
        public AliasContext? Aliases { get; set; }

        internal List<ParameterAccessor> ParameterAccessors = new();
        /// <summary>
        /// 编译好的结果
        /// </summary>
        public SentenceCmds cmds { get; set; }
    }
}
