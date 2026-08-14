using mooSQL.utils;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using static mooSQL.excel.ExcelBuilder;


namespace mooSQL.excel
{
    /// <summary>
    /// 导出的excel对象信息
    /// </summary>
    public class ExportTarget
    {
        /// <summary>
        /// 版本
        /// </summary>
        public string excelVersion = "2007";
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName;
        /// <summary>
        /// 数据
        /// </summary>
        public DataTable data;
        /// <summary>
        /// 作者
        /// </summary>
        public string Author = "文件作者信息"; //填加xls文件作者信息
        /// <summary>
        /// 创建程序信息
        /// </summary>
        public string ApplicationName = "创建程序信息"; //填加xls文件创建程序信息
        public string LastAuthor = "最后保存者信息"; //填加xls文件最后保存者信息
        public string AuthorInfo = "作者信息"; //填加xls文件作者信息
        public string Title = "标题信息"; //填加xls文件标题信息

        public string Theme = "主题信息";//填加文件主题信息
        public DateTime CreateDateTime = DateTime.Now;

        public string savePath;
        public bool hasTitle = false;
        public string contentTitle;
        //格式设置的部分信息
        public List<ExportCol> colInfos;

        public bool isInited = false;

        public int titleHeight = 28;
        public int colnameHeight = 23;
        public int colvalueHeight = 20;


        //样式自定义部分，分为标题、列名、列值3个。
        public IWorkbook book { get; set; }
        public ICellStyle titleStyle;
        public ICellStyle colNameStyle;
        public ICellStyle colValueStyle;
        public IDataFormat format;

        public ExportTarget() {
            colInfos= new List<ExportCol>();
        }
        /// <summary>
        /// 按表初始化
        /// </summary>
        /// <param name="dt"></param>
        public ExportTarget(DataTable dt)
        {
            this.data = dt;
            //this.init();
        }
        /// <summary>
        /// 表和版本
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="excelVersion"></param>
        public ExportTarget(DataTable dt, string excelVersion)
        {
            this.data = dt;
            this.excelVersion = excelVersion;
            //this.init();
        }
        /// <summary>
        /// 获取样式
        /// </summary>
        /// <returns></returns>
        public ICellStyle getCellStyle()
        {
            return book.CreateCellStyle();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public ExportTarget init()
        {
            this.isInited = true;
            if (excelVersion == "2007")
            {
                book = new NPOI.XSSF.UserModel.XSSFWorkbook();
            }
            else
            {
                book = new HSSFWorkbook();
            }
            format = book.CreateDataFormat();
            this.titleStyle = book.CreateCellStyle();
            this.colNameStyle = book.CreateCellStyle();
            this.colValueStyle = book.CreateCellStyle();
            //进行样式的缺省设置
            //大标题
            titleStyle.Alignment = HorizontalAlignment.Center;
            var font = book.CreateFont();
            font.FontHeightInPoints = 20;
            font.Boldweight = 700;
            titleStyle.SetFont(font);
            //列名
            colNameStyle.Alignment = HorizontalAlignment.Center;
            var fontc = book.CreateFont();
            fontc.FontHeightInPoints = 12;
            fontc.Boldweight = 700;
            colNameStyle.SetFont(fontc);
            colNameStyle.BorderBottom = BorderStyle.Thin;
            colNameStyle.BorderTop = BorderStyle.Thin;
            colNameStyle.BorderLeft = BorderStyle.Thin;
            colNameStyle.BorderRight = BorderStyle.Thin;
            colNameStyle.VerticalAlignment = VerticalAlignment.Center;
            //列值
            colValueStyle.Alignment = HorizontalAlignment.Left;
            colValueStyle.VerticalAlignment = VerticalAlignment.Center;
            var fontv = book.CreateFont();
            fontv.FontHeightInPoints = 12;
            colValueStyle.SetFont(fontv);
            colValueStyle.BorderBottom = BorderStyle.Thin;
            colValueStyle.BorderTop = BorderStyle.Thin;
            colValueStyle.BorderLeft = BorderStyle.Thin;
            colValueStyle.BorderRight = BorderStyle.Thin;

            colValueStyle.DataFormat = format.GetFormat("text");


            if (this.colInfos == null)
            {
                this.colInfos = new List<ExportCol>();
                foreach (DataColumn col in data.Columns)
                {
                    var coli = new ExportCol(col.ColumnName, col.Caption);
                    colInfos.Add(coli);
                }
            }
            foreach (var col in colInfos)
            {
                if (col.width == 0 && data.Columns.Contains(col.fieldName))
                {
                    col.width = Encoding.GetEncoding(936).GetBytes(data.Columns[col.fieldName].Caption.ToString()).Length;
                }
            }
            if (this.contentTitle == null)
            {
                this.contentTitle = Title;
            }
            //检查各行的最大宽度。

            for (int i = 0; i < colInfos.Count; i++)
            {
                var col = colInfos[i];
                if (col.style == null)
                {
                    col.style = book.CreateCellStyle();
                    col.style.CloneStyleFrom(colValueStyle);
                }
                if (col.fixedWidth) { continue; }
                for (int j = 0; j < data.Rows.Count; j++)
                {
                    int intTemp = Encoding.GetEncoding(936).GetBytes(col.getValue(data.Rows[j]) ).Length;
                    if (intTemp > col.width)
                    {
                        col.width = intTemp;
                    }
                }
            }
            //检查导出文件名
            if (string.IsNullOrWhiteSpace(FileName))
            {
                var now = DateTime.Now;
                FileName = "导出表格" + now.Year + "-" + now.Month + "-" + now.Day + (excelVersion == "2007" ? ".xlsx" : "xls");
            }

            return this;
        }

        public List<ISheet> sheets = new List<ISheet>();
        public ISheet currentSheet;
        public int currentRowIndex = 0;
        /// <summary>
        /// 初始化Excel文件的基础信息
        /// </summary>
        /// <returns></returns>
        public ExportTarget initBook() {
            if (this.excelVersion != "2007")
            {
                var realbook = book as HSSFWorkbook;
                if (realbook != null)
                {
                    DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                    dsi.Company = "NPOI";
                    realbook.DocumentSummaryInformation = dsi;

                    SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
                    si.Author = this.Author; //填加xls文件作者信息
                    si.ApplicationName = this.ApplicationName; //填加xls文件创建程序信息
                    si.LastAuthor = this.LastAuthor; //填加xls文件最后保存者信息
                    si.Comments = this.AuthorInfo; //填加xls文件作者信息
                    si.Title = this.Title; //填加xls文件标题信息

                    si.Subject = this.Theme;//填加文件主题信息
                    si.CreateDateTime = DateTime.Now;
                    realbook.SummaryInformation = si;
                    book = realbook;
                }

            }

            return this;
        }
        /// <summary>
        /// 新增一个sheet，并置为当前。
        /// </summary>
        /// <returns></returns>
        public ExportTarget newSheet(string name="") {
            if (this.book == null || !this.isInited)
            {
                this.init();
            }
            var workbook = this.book;
            if (string.IsNullOrWhiteSpace(name)) {
                name = "sheetauto" + (sheets.Count + 1);
            }
            var sheet = workbook.CreateSheet(name);

            this.currentSheet = sheet;
            this.sheets.Add(sheet);
            this.currentRowIndex = 0;
            return this;
        }
        /// <summary>
        /// 绘制大标题，并下移一行
        /// </summary>
        /// <returns></returns>
        public ExportTarget rendTitle()
        {
            // 表头及样式
            if (hasTitle)
            {
                var headerRow = currentSheet.CreateRow(currentRowIndex);
                headerRow.HeightInPoints = titleHeight;
                headerRow.CreateCell(0).SetCellValue(Title);

                headerRow.GetCell(0).CellStyle = titleStyle;
                currentSheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, colInfos.Count - 1));
                //headerRow.Dispose();
                currentRowIndex++;
            }
            return this;
        }
        /// <summary>
        /// 获取或者创建一个单元格
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public ICell getCell(int rowIndex, int colIndex) {
            if (this.currentSheet == null)
            {
                this.newSheet();
            }
            var row = currentSheet.GetRow(rowIndex);
            if (row == null)
            {
                row = currentSheet.CreateRow(rowIndex);
            }
            var cell = row.GetCell(colIndex);
            if (cell == null)
            {
                cell = row.CreateCell(colIndex);
            }
            return cell;
        }
        /// <summary>
        /// 设置一个单元格值，可选样式参数，默认为值样式，可选合并单元格参数
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="value"></param>
        /// <param name="cellStyle"></param>
        /// <param name="lastRow"></param>
        /// <param name="lastCol"></param>
        /// <returns></returns>
        public ExportTarget rendCell(int rowIndex, int colIndex, string value,ICellStyle cellStyle=null,int lastRow=-1,int lastCol=-1) { 
            var cell = getCell(rowIndex, colIndex);
            cell.SetCellValue(value);
            cell.SetCellType(CellType.String);
            if (cellStyle == null) { 
                cell.CellStyle = this.colValueStyle;
            }
            else
            {
                cell.CellStyle = cellStyle;
            }
            if(lastRow >= 0 ||lastCol>=0) {
                var range = new CellRangeAddress(rowIndex, lastRow > -1 ? lastRow : rowIndex, colIndex, lastCol > -1 ? lastCol : colIndex);
                currentSheet.AddMergedRegion(range);
            }
            return this;
        }
        /// <summary>
        /// 设置一个数值型的单元格
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="value"></param>
        /// <param name="cellStyle"></param>
        /// <param name="lastRow"></param>
        /// <param name="lastCol"></param>
        /// <returns></returns>
        public ExportTarget rendCell(int rowIndex, int colIndex, double value, ICellStyle cellStyle = null, int lastRow = -1, int lastCol = -1)
        {
            var cell = getCell(rowIndex, colIndex);
            cell.SetCellValue(value);
            if (cellStyle == null)
            {
                cell.CellStyle = this.colValueStyle;
            }
            else
            {
                cell.CellStyle = cellStyle;
            }
            if (lastRow >= 0 || lastCol >= 0)
            {
                var range = new CellRangeAddress(rowIndex, lastRow > -1 ? lastRow : rowIndex, colIndex, lastCol > -1 ? lastCol : colIndex);
                currentSheet.AddMergedRegion(range);
            }
            return this;
        }
        /// <summary>
        /// 设置一个日期型的单元格，有默认日期格式
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="value"></param>
        /// <param name="cellStyle"></param>
        /// <param name="lastRow"></param>
        /// <param name="lastCol"></param>
        /// <returns></returns>
        public ExportTarget rendCell(int rowIndex, int colIndex, DateTime value, ICellStyle cellStyle = null, int lastRow = -1, int lastCol = -1)
        {
            var cell = getCell(rowIndex, colIndex);
            cell.SetCellValue(value);
            cell.CellStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

            if (cellStyle == null)
            {
                cell.CellStyle = this.colValueStyle;
            }
            else
            {
                cell.CellStyle = cellStyle;
            }
            
            if (lastRow >= 0 || lastCol >= 0)
            {
                var range = new CellRangeAddress(rowIndex, lastRow > -1 ? lastRow : rowIndex, colIndex, lastCol > -1 ? lastCol : colIndex);
                currentSheet.AddMergedRegion(range);
            }
            return this;
        }

        /// <summary>
        /// 绘制标题行并下移一行
        /// </summary>
        /// <returns></returns>
        public ExportTarget rendHead()
        {
            var headerRow = currentSheet.CreateRow(currentRowIndex);
            headerRow.HeightInPoints = this.colnameHeight;
            for (int c = 0; c < this.colInfos.Count; c++)
            {
                var col = this.colInfos[c];
                var cell = headerRow.CreateCell(c);
                cell.SetCellValue(col.caption);
                cell.CellStyle = this.colNameStyle;

                //设置列宽
                int widtd = 250;
                if (col.width < widtd)
                {
                    widtd = col.width + 2;
                }
                currentSheet.SetColumnWidth(c, widtd * 256);
                if (col.show == false)
                {
                    currentSheet.SetColumnHidden(c, true);
                }
            }
            //headerRow.Dispose();
            currentRowIndex++;
            return this;
        }
        /// <summary>
        /// 根据dataTable 绘制表体
        /// </summary>
        /// <returns></returns>
        public ExportTarget rendBody()
        {
            foreach (DataRow row in data.Rows)
            {
                var dataRow = currentSheet.CreateRow(currentRowIndex);
                dataRow.HeightInPoints = colvalueHeight;
                //foreach (DataColumn column in dtSource.Columns)
                for (int c = 0; c < colInfos.Count; c++)
                {
                    var col = colInfos[c];
                    var newCell = dataRow.CreateCell(c);
                    newCell.CellStyle = col.style;

                    string drValue = col.getValue( row);
                    newCell.SetCellValue(drValue);
                    var typename = col.typeName;
                    if (string.IsNullOrWhiteSpace(typename))
                    {
                        typename = data.Columns[col.fieldName].DataType.Name;
                    }
                    //var mycellstyle = workbook.CreateCellStyle();
                    //mycellstyle.CloneStyleFrom(tar.colValueStyle);
                    //switch (type.ToString())
                    switch (typename)
                    {
                        case "Guid":
                        case "String"://字符串类型
                            //newCell.SetCellValue(drValue);
                            newCell.SetCellType(CellType.String);
                            break;
                        case "DateTime"://日期类型
                            DateTime dateV;
                            if (DateTime.TryParse(drValue, out dateV))
                            {
                                newCell.SetCellValue(dateV);
                            }
                            newCell.CellStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
                            break;
                        case "Boolean"://布尔型
                            bool boolV = false;
                            bool.TryParse(drValue, out boolV);
                            newCell.SetCellValue(boolV);
                            newCell.SetCellType(CellType.Boolean);
                            break;
                        case "Int16"://整型
                        case "Int32":
                        case "Int64":
                        case "Byte":
                            int intV = 0;
                            int.TryParse(drValue, out intV);
                            //newCell.SetCellValue(intV);
                            //mycellstyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
                            //mycellstyle.DataFormat= format.GetFormat("0");
                            //newCell.CellStyle.DataFormat = 0;
                            newCell.SetCellType(CellType.String);
                            break;
                        case "Decimal"://浮点型
                        case "Double":
                            double doubV = 0;
                            double.TryParse(drValue, out doubV);
                            //newCell.SetCellValue(doubV);

                            //newCell.CellStyle.DataFormat = format.GetFormat("0.00");
                            newCell.SetCellType(CellType.String);
                            break;
                        case "DBNull"://空值处理
                            newCell.SetCellValue("");

                            break;
                        default:
                            newCell.SetCellValue(drValue);
                            newCell.CellStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("@");
                            newCell.SetCellType(CellType.String);

                            break;
                    }
                }
                currentRowIndex++;
            }
            return this;
        }


        public MemoryStream toStream()
        {

            MemoryStream ms = new MemoryStream(toBytes());
            return ms;
            
        }

        public byte[] toBytes()
        {

            using (NpoiMemoryStream ms = new NpoiMemoryStream())
            {
                ms.AllowClose = false;
                book.Write(ms);
                ms.Flush();
                ms.Position = 0;
                ms.AllowClose = true;

                //sheet.Dispose();
                //workbook.Dispose();//一般只用写这一个就OK了，他会遍历并释放所有资源，但当前版本有问题所以只释放sheet
                return ms.ToArray();
            }
        }
        /// <summary>
        /// 写入到某个文件夹，返回文件路径。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string writeToFile(string path, string fileName) { 
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (path.EndsWith("/") == false) {
                path= path+"/";
            }

            var filePath= path + fileName;
            var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            book.Write(fileStream);
            fileStream.Close();
            fileStream.Dispose();
            return filePath;
        }
        
    }
}
