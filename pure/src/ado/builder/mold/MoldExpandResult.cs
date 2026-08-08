using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// L2 展开结果：替换模版中的占位片段，并产出最终参数。
    /// </summary>
    public sealed class MoldExpandResult
    {
        /// <summary>写入 SQL 的片段（如 <c>@mold_0_0,@mold_0_1</c> 或空）。</summary>
        public string SqlFragment { get; set; }

        /// <summary>展开后的参数（key 不含方言前缀）。</summary>
        public List<KeyValuePair<string, object>> Parameters { get; set; }

        /// <summary>
        /// 创建空展开结果。
        /// </summary>
        public MoldExpandResult()
        {
            SqlFragment = "";
            Parameters = new List<KeyValuePair<string, object>>();
        }

        /// <summary>
        /// 创建带片段的展开结果。
        /// </summary>
        public MoldExpandResult(string sqlFragment)
        {
            SqlFragment = sqlFragment ?? "";
            Parameters = new List<KeyValuePair<string, object>>();
        }
    }
}
