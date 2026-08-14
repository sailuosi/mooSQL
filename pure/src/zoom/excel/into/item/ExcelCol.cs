
using System.Collections.Generic;

using System.Text.RegularExpressions;


namespace mooSQL.excel.context
{
    /// <summary>
    /// excel列信息读取结果储存类
    /// 包含，列标题信息，列编号，列指针，列关联的写入列对象。
    /// </summary>
    public class ExcelCol
    {
        /// <summary>
        /// 列号
        /// </summary>
        public string code;
        /// <summary>
        /// 列索引
        /// </summary>
        public int index;
        /// <summary>
        /// excel 行和对应的列值。
        /// </summary>
        public Dictionary<int, string> titles = new Dictionary<int, string>();
        /// <summary>
        /// 是否符合
        /// </summary>
        /// <param name="matches"></param>
        /// <returns></returns>
        public bool IsYour(List<Regex> matches)
        {
            bool res = false;
            foreach (var ma in matches)
            {
                bool finded = false;
                foreach (var tt in titles)
                {
                    if (ma.IsMatch(tt.Value))
                    {
                        finded = true;
                        break;
                    }
                }
                if (!finded)
                {
                    return false;
                }
                res = true;
            }
            return res;
        }
    }
}
