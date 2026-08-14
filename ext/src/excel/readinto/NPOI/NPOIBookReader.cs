

using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 依赖于NPOI库的excel读取器
    /// </summary>
    public class NPOIBookReader : IBookReader
    {

        private XWorkBook xbook;
        private IWorkbook workbook;

        private ReadScopeConfig readeScopeConfig;
        private Stream readingStream;
        /// <summary>
        /// 
        /// </summary>
        public NPOIBookReader() { 
            xbook = new XWorkBook();
        }


        public IBookReader useStream(Stream stream)
        {
            this.readingStream = stream;
            return this;
        }

        IBookReader IBookReader.useScope(ReadScopeConfig config)
        {
            this.readeScopeConfig= config;
            return this;
        }
        /// <summary>
        /// 要读取的表
        /// </summary>
        /// <param name="workbook"></param>
        /// <returns></returns>
        public NPOIBookReader useBook(IWorkbook workbook)
        { 
            this.workbook = workbook;
            return this;
        }
        /// <summary>
        /// 读取范围
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public NPOIBookReader useRange(ReadScopeConfig config)
        {
            this.readeScopeConfig = config;
            return this;
        }

        private IFormulaEvaluator evaluator;

        private NPOIBookReader readBook() {

            if (workbook == null)
            {
                return this;
            }

            if (this.xbook == null) { 
                xbook = new XWorkBook();
            }
            //检查读取配置
            if (this.readeScopeConfig == null) { 
                var config= new ReadScopeConfig();
                config.readAll=true;
                this.readeScopeConfig = config;
            }
            evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
            evaluator.EvaluateAll();
            //逐个读取sheet
            for (var s = 0; s < workbook.NumberOfSheets; s++) {
                if (readeScopeConfig.containSheet(s) == false) {
                    continue;
                }
                var sheet=workbook.GetSheetAt(s);
                this.readSheet(sheet, s);
            }
            return this;
        }

        private NPOIBookReader readSheet(ISheet sheet,int sheetIndex) {
            if (sheet == null)
            {
                return this;
            }
            //准备好待写入的表信息对象

            var xsheet= xbook.getOrNew(sheetIndex);
            
            var sheetScope= readeScopeConfig.getSheetScope(sheetIndex);

            var maxCol= sheetScope.getMaxAZ();

            int rowCount = sheet.LastRowNum + 1;//总行数            
            sheet.ForceFormulaRecalculation = true;
            for (var i = 0; i < rowCount; i++) {
                //读取的行范围配置
                if (sheetScope.containsRow(i+1) == false) {
                    continue;
                }
                IRow row = sheet.GetRow(i);
                if (row == null || row.Cells == null)
                {
                    continue;
                }
                var xrow = xsheet.getOrNew(i);
                if (row == null) {
                    continue;
                }

                for (var c = 0; c < row.Cells.Count && c<maxCol; c++)
                {
                    //暂未实现：结合行范围、列范围进行检查
                    if (sheetScope.containsCol(c + 1) == false)
                    {
                        continue;
                    }
                    var cell = row.Cells[c];
                    if (cell == null) continue;
                    ICell mcell;
                    XCell cellval=null;
                    if (NPOIExcelUntils.IsMergeCell(sheet, i, c, out mcell))
                    {
                        cellval = NPOIExcelUntils.CellToXCell(mcell,evaluator);
                    }
                    else
                    {
                        cellval = NPOIExcelUntils.CellToXCell(cell, evaluator);
                    }
                    
                    xrow.addCell(cell.ColumnIndex, cellval);
                }
            }
            return this;
        }
        /// <summary>
        /// 转为内置的表格
        /// </summary>
        /// <returns></returns>
        public XWorkBook asBook()
        {
            this.readBook();
            return this.xbook;
        }

    }
}
