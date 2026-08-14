using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// Excel的一个sheet
    /// </summary>
    public class XSheet
    {
        /// <summary>
        /// 行集合
        /// </summary>
        public Dictionary<int, XRow> Rows;
        /// <summary>
        /// 构造函数
        /// </summary>
        public XSheet()
        {

            Rows = new Dictionary<int, XRow>();
        }
        /// <summary>
        /// 行数
        /// </summary>
        public int RowCount { 
            get { return Rows.Count; }
        }
        /// <summary>
        /// 获取行对象
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        public XRow getRow(int rowIndex) { 
            if(Rows!=null && Rows.ContainsKey(rowIndex)) return Rows[rowIndex];
            return null;
        }
        /// <summary>
        /// 获取单元格对象
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public XCell getCell(int rowIndex, int colIndex) { 
            var row=getRow(rowIndex);
            if(row==null) return null;
            return row.getCell(colIndex);
        }
        /// <summary>
        /// 获取行对象，如果不存在则新建一个
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        public XRow getOrNew(int rowIndex)
        {
            if (Rows.ContainsKey(rowIndex) == false)
            {
                Rows.Add(rowIndex, new XRow());
            }
            var xsheet = Rows[rowIndex];
            if (xsheet == null)
            {
                Rows[rowIndex] = new XRow();
                xsheet = Rows[rowIndex];
            }
            return xsheet;
        }
    }
}
