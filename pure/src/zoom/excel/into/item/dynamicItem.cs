
using System.Collections.Generic;


namespace mooSQL.excel.context
{
    /// <summary>
    /// 动态范围对象
    /// </summary>
    public class dynamicItem
    {
        /// <summary>
        /// 列循环的列标题
        /// </summary>
        public List<string> headCols = new List<string>();//一个转置列的所有标题列。
        /// <summary>
        /// 该列的列号，如AA
        /// </summary>
        public string focusHeadCode;
        /// <summary>
        /// 交叉列的中心值。
        /// </summary>
        public string crossValue;
        /// <summary>
        /// 动态列列值的核心列。
        /// </summary>
        public string dynamicKey;
    }
}
