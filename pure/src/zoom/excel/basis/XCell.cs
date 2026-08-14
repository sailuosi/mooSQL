using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /*
     * * 在excel文件中，v中存放的实际是索引，真正的值放在 xl/sharedStrings.xml中。所以相同的字符串，实际上会被只存储一份，单元格拥有其索引。
     */
    /// <summary>
    /// 单元格类
    /// </summary>
    public class XCell
    {
        /// <summary>
        /// Excel中的 r 属性，如A86
        /// </summary>
        public string code;
        /// <summary>
        /// Excel中的 s 属性,如 1、2、3、4
        /// </summary>
        public string style;
        /// <summary>
        /// 值类型。如s 
        /// </summary>
        public string type;

        /// <summary>
        /// 值属性 v 
        /// </summary>
        public string value;
        /// <summary>
        /// 行列索引，方便后续处理
        /// </summary>
        public int rowIndex;
        /// <summary>
        /// 行列索引，方便后续处理
        /// </summary>
        public int columnIndex;
        /// <summary>
        /// 值类型对应的实际对象，方便后续处理。例如：数字、日期等
        /// </summary>
        public object typeValue;
    }
}
