// 基础功能说明：

using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;


namespace mooSQL.excel {
    /// <summary>
    /// excel导出构建器
    /// </summary>
    public class ExportBuilder
    {

        private ExportTarget buildContext=new ExportTarget();
        /// <summary>
        /// 设置数据集
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public ExportBuilder setData(DataTable table)
        {
            buildContext.data = table;
            return this;
        }
        /// <summary>
        /// 设置Excel版本 为 2007、2003
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public ExportBuilder setVersion(string version)
        {

            return this;
        }
        /// <summary>
        /// 添加一个导出列
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="caption"></param>
        /// <param name="width"></param>
        /// <param name="codeTableID"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public ExportBuilder add(string columnName,string caption,int width=0,string codeTableID="", ICellStyle style=null, Dictionary<string, string> valueMap=null)
        {
            var col = new ExportCol(columnName, caption);
            col.width = width;
            if (col.width > 0) { 
                col.fixedWidth = true;
            }
            col.codeTableID = codeTableID;
            col.style = style;
            if(valueMap!=null && valueMap.Count > 0)
            {
                col.valueMap = valueMap;
            }
            buildContext.colInfos.Add(col);
            return this;
        }
        /// <summary>
        /// 设置单元格的值，可指定样式、合并边界。从0开始的索引
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="value"></param>
        /// <param name="cellStyle"></param>
        /// <param name="lastRow"></param>
        /// <param name="lastCol"></param>
        /// <returns></returns>
        public ExportBuilder rendCell(int rowIndex, int colIndex, string value, ICellStyle cellStyle = null, int lastRow = -1, int lastCol = -1)
        {
            buildContext.rendCell(rowIndex, colIndex, value, cellStyle, lastRow, lastCol);
            return this;
        }
        /// <summary>
        /// 设置单元格的值，可指定样式、合并边界。从0开始的索引
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="value"></param>
        /// <param name="cellStyle"></param>
        /// <param name="lastRow"></param>
        /// <param name="lastCol"></param>
        /// <returns></returns>
        public ExportBuilder rendCell(int rowIndex, int colIndex, double value, ICellStyle cellStyle = null, int lastRow = -1, int lastCol = -1)
        {
            buildContext.rendCell(rowIndex, colIndex, value, cellStyle, lastRow, lastCol);
            return this;
        }
        /// <summary>
        /// 设置单元格的值，可指定样式、合并边界。从0开始的索引
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="value"></param>
        /// <param name="cellStyle"></param>
        /// <param name="lastRow"></param>
        /// <param name="lastCol"></param>
        /// <returns></returns>
        public ExportBuilder rendCell(int rowIndex, int colIndex, DateTime value, ICellStyle cellStyle = null, int lastRow = -1, int lastCol = -1)
        {
            buildContext.rendCell(rowIndex, colIndex, value, cellStyle, lastRow, lastCol);
            return this;
        }

        /// <summary>
        /// 按照当前的列信息和数据表，绘制到当前的sheet中
        /// </summary>
        /// <returns></returns>
        public ExportBuilder rend()
        {
            if (buildContext.currentSheet == null) {
                buildContext.newSheet();
            }
            buildContext
                .rendTitle()
                .rendHead()
                .rendBody();

            return this;
        }

        #region excel操作
        /// <summary>
        /// 新建一个sheet
        /// </summary>
        /// <returns></returns>
        public ExportBuilder newSheet(string name=null) { 
            buildContext .newSheet(name);
            return this;
        }
        /// <summary>
        /// 设置当前sheet的焦点行
        /// </summary>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        public ExportBuilder setRowIndex(int currentIndex)
        {
            buildContext.currentRowIndex = currentIndex;
            return this;
        }

        /// <summary>
        /// 输出流
        /// </summary>
        /// <returns></returns>
        public MemoryStream toSream()
        {
            
            return buildContext.toStream();
        }

        public byte[] toBytes()
        {

            return buildContext.toBytes();
        }

        public string writeToFile(string path, string fileName) { 
        
            return buildContext.writeToFile(path, fileName);
        }
        #endregion


    }
}

