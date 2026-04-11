using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>JOIN 的 ON/USING 条件包装。</summary>
    public class JoinOnWord
    {

        /// <summary>搜索条件（WHERE 语法子集）。</summary>
        public SearchConditionWord condition;

        /// <summary>空条件（如 CROSS JOIN）。</summary>
        public JoinOnWord() { }

        /// <summary>指定 ON 条件。</summary>
        public JoinOnWord(SearchConditionWord condition) { this.condition = condition; }    
    }
}
