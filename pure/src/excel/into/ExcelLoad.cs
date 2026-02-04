using mooSQL.excel.context;

using mooSQL.utils;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// Excel导入过程中 表格信息交互部分的数据
    /// </summary>
    public abstract class ExcelLoad:ExcelRead
    {
        /// <summary>
        /// 用户唯一标记 ，用来查询进度使用
        /// </summary>
        /// <param name="token"></param>
        protected ExcelLoad(string token) : base(token)
        {

        }

        /// <summary>
        /// 表体对象
        /// </summary>
        public XWorkBook excel;
        /// <summary>
        /// 正在读取的sheet
        /// </summary>
        public XSheet readingSheet;
        /// <summary>
        /// 读取excel对象的数据
        /// </summary>
        /// <param name="workbook">表格对象</param>
        /// <returns>存储数据的datatable</returns>
        public DataTable readExcelData(XWorkBook workbook)
        {
            DataTable dataTable = new DataTable();
            if (workbook == null)
            {
                return dataTable;
            }
            this.excel = workbook;
            if (context.option.onBeforeReadExcel != null)
            {
                var res = context.option.onBeforeReadExcel(workbook, dataTable, this);
            }

            this.pushLog("准备导入Excel文件.\n<br/>", "tip");
            var sheet = workbook.getSheet(0);//读取第一个sheet，当然也可以循环读取每个sheet

            if (sheet != null)
            {
                this.readingSheet = sheet;

                if (context.option.onBeforeReadSheet != null)
                {
                    var res = context.option.onBeforeReadSheet(sheet, dataTable, this);
                }
                int rowCount = sheet.RowCount;//总行数
                if (rowCount < 1)
                {
                    this.pushLog("Excel文件中只有列标题，不能导入.\n<br/>", "tip");
                    return dataTable;
                }

                var firstRow = sheet.getRow(0);//第一行是列名
                var threeRow = sheet.getRow(1);//第二行是数据，在这里是为了获取列的类型
                int cellCount = excelTitleRow.Count;//列数
                if (excelTitleRow.Count < 1)
                {
                    this.pushLog("Excel文件中列数据太少，不能导入.\n<br/>", "");
                    return dataTable;
                }
                //构建datatable的列
                //todo 超前解析标题所在行。
                if (context.option.scanTitle)
                {
                    var ttReg = context.option.titleScanReg;
                    if (ttReg.Count == 0)
                    {
                        ttReg = this.getAllTitleMatchReg();
                    }
                    if (ttReg.Count > 0)
                    {
                        //存在有效的匹配规则，执行扫描。
                        string tip = "";
                        var totalReg = new Regex(string.Format("({0})", string.Join(")|(", ttReg)));
                        titlsScanScope.ForEachClosed((s) => {
                            var ri = s - 1;
                            var row = sheet.getRow(ri);
                            foreach (var kv in row.cells)
                            {
                                if (kv.Value == null) continue;
                                var cellval = kv.Value.value;
                                if (totalReg.IsMatch(cellval))
                                {
                                    excelTitleRow.addSolo(s);
                                    tip += s + ",";
                                    break;
                                }
                            }
                        });
                        pushLog(string.Format("标题行自动检测结束，发现存在标题的行：{0}", tip), "important");
                    }
                }


                int colIndex = 0;

                excelTitleRow.ForEachClosed((t) => {
                    var ri = t - 1;
                    var row = sheet.getRow(ri);
                    if (row == null) return;

                    //for (int c = 0; c < row.Cells.Count; c++)
                    foreach (var kv in row.cells)
                    {
                        var cell=kv.Value;
                        //excel的行集合cells不包含空列，只有其colIndex指向真实位置
                        var c = cell.columnIndex;
                        var colCap = "";
                        if (cell != null)
                        {   //当单元格是合并单元格时，取其合并值。
  
                            colCap =cell.value;
                            
                            if (!string.IsNullOrWhiteSpace(colCap) && c > lastTitleNum)
                            {
                                lastTitleNum = c;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        var colcode =ExcelUntil.getExcelColCode(c);
                        addExcelTitleInfo(t, colcode, colCap);
                        if (excelCols.ContainsKey(colcode)) excelCols[colcode].index = c;

                        var cellCode = colcode + t;
                        if (readedColIndex.Contains(c) == false)
                        {   //创建数据列
                            var col = new DataColumn(colcode);
                            col.Caption = colCap;

                            dataTable.Columns.Add(col);
                            readedColIndex.Add(c);
                            //.
                        }
                        //创建列定义信息列
                        if (context.valueCollection.contain(cellCode) == false)
                        {
                            var col = new colInfo(cellCode, this);
                            col.root = this;
                            col.type = columnType.head;
                            col.caption = colCap;
                            col.colIndex = c;
                            col.rowIndex = t;
                            col.value = colCap;
                            col.writeValue = colCap;
                            col.state = "done";//等同已解析类
                            context.valueCollection.addCol(cellCode, col);
                        }

                        foreach (var kvCol in context.colMap)
                        {
                            var col = kvCol.Value;
                            if (col.ExcelIndex != -1 || col.type != columnType.match) continue;
                            //|| reg.IsMatch(colCap) || colCap.IndexOf(matchstr) != -1
                            if ((isValid(col.excelCol) && isMatch(colCap, col.excelCol)) || (isValid(col.excelCode) && col.excelCode == colcode))
                            {
                                col.ExcelIndex = c;
                                col.excelCode = colcode;
                                if (excelCheckColnames.Contains(col.key) && !excelCheckColIndex.Contains(colIndex))
                                {//顺便判断该列是不是需要储存值的列。
                                    addExcelCheckColIndex(colIndex);
                                }
                                //break;//改为继续搜索，这样就允许同时定义多个字段取一个列。
                            }
                        }
                        colIndex++;
                    }
                });

                if (context.option.onAfterMatchTitle != null)
                {
                    context.option.onAfterMatchTitle(workbook);
                }

                //检查是否存在cell列，存在则取值
                foreach (var kv in context.colMap)
                {
                    if (kv.Value.type == columnType.cell && isValid(kv.Value.option.cell))
                    {
                        //var tar=sheet.
                        int rowii, colii;
                        if (ExcelUntil.parseCellCode(kv.Value.option.cell, out rowii, out colii))
                        {
                            var cell = sheet.getCell(rowii, colii);
                            if (cell != null)
                            {
                                var val =cell.value;
                                if (val != null)
                                {
                                    kv.Value.writeValue = val.ToString();
                                    kv.Value.state = "done";
                                    if (kv.Value.needGather)
                                    {
                                        kv.Value.uniValues.AddNotRepeat( val.ToString());
                                    }
                                }
                            }
                        }
                        if (isValid(kv.Value.writeValue) == false && kv.Value.option.showTip)
                        {
                            var msg = string.Format("列{0}在excel单元格{1}中未发现值！", kv.Value.caption, kv.Value.option.cell);
                            if (isValid(kv.Value.option.defaultValue))
                            {
                                kv.Value.writeValue = kv.Value.option.defaultValue;
                                msg += "自动启用其默认值" + kv.Value.option.defaultValue;
                            }
                            pushLog(msg, "tip");
                        }
                        kv.Value.state = "done";
                    }
                    //再次执行列匹配。此时允许多列组合确定。
                    else if (kv.Value.type == columnType.match && kv.Value.ExcelIndex == -1 && kv.Value.matchRegs.Count > 0)
                    {
                        foreach (var exceltt in excelCols)
                        {
                            if (exceltt.Value.IsYour(kv.Value.matchRegs))
                            {
                                kv.Value.ExcelIndex = exceltt.Value.index;
                                kv.Value.excelCode = exceltt.Value.code;
                                if (excelCheckColnames.Contains(kv.Key) && !excelCheckColIndex.Contains(exceltt.Value.index))
                                {//顺便判断该列是不是需要储存值的列。
                                    addExcelCheckColIndex(exceltt.Value.index);
                                }
                            }
                        }

                    }
                }

                //数据体循环 "1,5,6-10,12" 格式
                //准备执行超前数据检查。
                this.preparePreExcelMatch();



                //for (int d = sheet.FirstRowNum; d <= sheet.LastRowNum; d++)
                foreach(var kvRow in sheet.Rows)
                {
                    var sheetRowIndex = kvRow.Key + 1;
                    if (excelDataRow.Contain(sheetRowIndex))
                    {

                        this.readExcelRowToDt(dataTable, sheet, kvRow.Key);
                    }
                }
                if (context.option.onBeforeReadExcel != null)
                {
                    var res = context.option.onBeforeReadExcel(workbook, dataTable, this);
                }
            }

            if (context.option.onAfterReadExcel != null) { 
                context.option.onAfterReadExcel(workbook, dataTable,this);
            }
            return dataTable;
        }


        /// <summary>
        /// 将一行excel数据读取到dataTable中
        /// </summary>
        /// <param name="dt">存储excel数据的datatable</param>
        /// <param name="sheet">excel的sheet</param>
        /// <param name="rowIndex">行指针</param>
        private void readExcelRowToDt(DataTable dt, XSheet sheet, int rowIndex)
        {

            if (excelRows.ContainsKey(rowIndex)) return;
            var row = sheet.getRow(rowIndex);
            if (row == null || row.Count == 0) return;
            if (context.option.onBeforeReadExcelRow != null)
            {
                if (!context.option.onBeforeReadExcelRow(dt, row, rowIndex)) return;
            }

            var dataRow = dt.NewRow();
            //for (int j = 0; j < row.Cells.Count; j++)
            foreach (var j in readedColIndex)
            {
                //var cell = row.Cells[j];
                bool needPreMatch = excelPreMatches.ContainsKey(j);
                var cell = sheet.getCell(rowIndex, j);
                var colcode =ExcelUntil.getExcelColCode(j);
                if (dt.Columns.Contains(colcode) == false)
                {
                    var col = new DataColumn(colcode);
                    col.Caption = colcode;
                    dt.Columns.Add(col);
                }
                if (cell == null)
                {
                    dataRow[colcode] = "";
                    if (needPreMatch)
                    {
                        pushLog(string.Format("发现表中第{0}行第{1}列数据缺失，该行不予导入，请核查数据。", rowIndex + 1, colcode), "important");
                        return;
                    }
                }
                else
                {
                    //CellType(Unknown = -1,Numeric = 0,String = 1,Formula = 2,Blank = 3,Boolean = 4,Error = 5,)
                    var cellvalue = cell.typeValue;
                    if (context.option.onLoadCellValue != null)
                    {
                        cellvalue = context.option.onLoadCellValue(cellvalue, cell);
                    }
                    if (cellvalue == null)
                    {
                        if (needPreMatch)
                        {
                            pushLog(string.Format("发现表中第{0}行第{1}列数据缺失，该行不予导入，请核查数据。", rowIndex + 1, colcode), "important");
                            return;
                        }
                        continue;
                    }
                    var strVal = cellvalue.ToString();
                    if (needPreMatch)
                    {
                        foreach (var co in excelPreMatches[j])
                        {
                            if (co.option.onCheckCellValue != null)
                            {
                                if (!co.option.onCheckCellValue(strVal)) return;
                            }
                            else if (Regex.IsMatch(strVal, co.option.preMatchReg) == false)
                            {
                                //核验未通过。
                                var msg = string.Format("发现表中第{0}行第{1}列数据{2}，该行不予导入，请核查数据。", rowIndex + 1, colcode, co.option.preMatchMsg);
                                pushLog(msg, "important");
                                return;
                            }
                        }
                    }

                    dataRow[colcode] = cellvalue;
                    if (excelCheckColIndex.Contains(j))
                    {//储存列值到库，为备查做准备。
                        addExcelColValue(j, cellvalue.ToString());
                    }
                }
            }
            dt.Rows.Add(dataRow);
            var rowi = new rowInfo(sheet.getRow(rowIndex), dataRow);
            rowi.dataRowIndex = dt.Rows.Count;
            rowi.excelRowIndex = rowIndex;
            rowi.excelRowNum = rowIndex + 1;
            excelRows[rowIndex] = rowi;
            //readedRowIndex.Add(rowIndex);
            //excelRowMap[dt.Rows.Count-1] =row.RowNum + 1;
        }


        /// <summary>
        /// 保存消息到表格
        /// </summary>
        public override void saveMsgToExcel()
        {
            //因该方法的结果完全不影响导入本身，所以拆开执行。
            try
            {

                int lastRow = 0;
                var msgCol = lastTitleNum + 1;
                var optCode = context.option.logColNum;
                if (!string.IsNullOrWhiteSpace(optCode))
                {
                    var icode = ExcelUntil.parCellColCode(optCode);
                    if (icode >= 0)
                    {
                        msgCol = icode;
                    }
                }
                foreach (var excelRow in excelRows)
                {
                    var erow = excelRow.Value.excelRow;
                    erow.addCell(msgCol,excelRow.Value.rowMsg);
                    if (excelRow.Value.excelRowIndex > lastRow)
                    {
                        lastRow = excelRow.Value.excelRowIndex;
                    }
                }
                if (this.readingSheet != null)
                {
                    var lastERow = readingSheet.getOrNew(lastRow + 1);
                    lastERow.addCell(msgCol, totalMsg);
                }
                this.SaveWorkbook(excel, excelFilePath);

            }
            catch (Exception e)
            {
                this.pushLog("更新导入信息到表格中失败，将无法下载导入的原表", "important");
            }
        }

        public virtual void SaveWorkbook(XWorkBook wb, string fileName)
        {
            /*
            using (var fs = new NpoiFileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.AllowClose = false;
                wb.Write(fs);
                fs.Position = 0;
                fs.Flush();
                fs.AllowClose = true;
                fs.Close();
            }*/
        }



        /*
  * 主要提供基本的excel操作，和简单的 list  map的操作封装
 */
        /// <summary>
        /// 某行表格数据是否包含某个值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsCellValue(XRow row, string value)
        {
            if (row == null) return false;
            foreach (var kv in row.cells)
            {
                var cv = kv.Value.value;
                if (cv == value)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 某行表格数据是否能按正则匹配某个值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="reg"></param>
        /// <returns></returns>
        public bool ContainsCellValue(XRow row, Regex reg)
        {
            if (row == null) return false;
            foreach (var cell in row.cells)
            {
                var cv = cell.Value.value;
                if (reg.IsMatch(cv))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
