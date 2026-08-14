
using System.Data;


namespace mooSQL.excel.context
{
    /// <summary>
    /// 记录数据体行信息
    /// </summary>
    public class rowInfo
    {
        /// <summary>
        /// 从0开始的excel行指针
        /// </summary>
        public int excelRowIndex = 0;
        /// <summary>
        /// 从1开始的真实用户行号；
        /// </summary>
        public int excelRowNum = 1;
        /// <summary>
        /// 数据在dataTable中的真实索引
        /// </summary>
        public int dataRowIndex = 0;
        /// <summary>
        /// 对应的导入Excel类的行
        /// </summary>
        public XRow excelRow;
        /// <summary>
        /// 数据体行
        /// </summary>
        public DataRow dataRow;
        /// <summary>
        /// 行位置标记文字
        /// </summary>
        public string rowMark = "";
        /// <summary>
        /// 行处理消息
        /// </summary>
        public string rowMsg = "";
        /// <summary>
        /// 空标记
        /// </summary>
        public bool empty = false;
        /// <summary>
        /// 行数据信息
        /// </summary>
        public rowInfo()
        {
            empty = true;
        }
        /// <summary>
        /// 行数据信息
        /// </summary>
        /// <param name="exrow"></param>
        /// <param name="dtrow"></param>
        public rowInfo(XRow exrow, DataRow dtrow)
        {
            this.excelRow = exrow;
            this.dataRow = dtrow;
            this.excelRowIndex = exrow.rowIndex;
            this.excelRowNum = exrow.rowIndex + 1;
        }
    }
}
