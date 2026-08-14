using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 对应xml中的row标签
    /// </summary>
    public class XRow
    {
        /// <summary>
        /// 初始化空单元格字典。
        /// </summary>
        public XRow() { 
            cells= new Dictionary<int, XCell> ();
        }
        /// <summary>
        /// 行编号，为数值，如77，对应xml中的r属性
        /// </summary>
        public string code;
        /// <summary>
        /// 行跨度，如1:9，表示A-I列。
        /// </summary>
        public string spans;
        /// <summary>
        /// 高度，对应ht属性，为行高，值为数值，如19
        /// </summary>
        public string height;
        /// <summary>
        /// 是否自定义高度，值为1表示是，
        /// </summary>
        public string customHeight;
        /// <summary>
        /// 对应同名xml属性，值如1,
        /// </summary>
        public string thickBot;

        /// <summary>行在表中的从 0 或 1 起的序号（与同模块约定一致）。</summary>
        public int rowIndex;

        /// <summary>列索引到单元格的映射。</summary>
        public Dictionary<int,XCell> cells;
        /// <summary>
        /// 单元格数
        /// </summary>
        public int Count
        {
            get {
                if (cells == null) { 
                    return 0;
                }
                return cells.Count;
            }
        
        }

        /// <summary>按列索引获取单元格，不存在则返回 null。</summary>
        public XCell getCell(int index) {
            if (cells != null && cells.ContainsKey(index)) { 
                return cells[index];
            }
            return null;
        }

        /// <summary>获取或创建指定列索引的单元格。</summary>
        public XCell getOrNew(int index)
        {
            if (cells != null && cells.ContainsKey(index))
            {
                return cells[index];
            }
            var cell = new XCell(); 
            cell.rowIndex = index;

            cells[index]= cell;
            return cell;
        }

        /// <summary>设置列上的单元格引用。</summary>
        public XRow addCell(int colIndex, XCell cell) {
            
            cells[colIndex] = cell;
            return this;
        }

        /// <summary>用对象值创建单元格并放入该行。</summary>
        public XRow addCell(int colIndex, object value)
        {
            var cell = new XCell();
            cell.rowIndex = this.rowIndex;
            cell.columnIndex = colIndex;
            cell.value=value.ToString();
            cell.typeValue = value;
            cells[colIndex] = cell;
            return this;
        }
    }
}
