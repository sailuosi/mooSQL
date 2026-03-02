using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// SQL补丁类，用于构建SQL语句时使用，提供一些附加条件和操作的封装。
    /// </summary>
    public class SQLMakeUps
    {
        /// <summary>
        /// 汇总字段列表，用于构建SQL语句时使用。
        /// </summary>
        public List<string> summaryField { get; set; }

        public void clear()
        {
            if (summaryField != null)
            {
                summaryField.Clear();
            }
        }

        public void addSummaryField(string field)
        {
            if (summaryField == null)
            {
                summaryField = new List<string>();
            }
            summaryField.Add(field);
        }

        internal string getSummaryFieldSQL()
        {
            if (summaryField != null && summaryField.Count > 0)
            {
                return summaryField.JoinNotEmpty(", ");
            }
            return string.Empty;
        }
    }
}
