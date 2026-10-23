using System;
using System.Collections.Generic;
using System.Text;
using System.Data;


using System.IO;

using NPOI.HPSF;

using NPOI.HSSF.UserModel;


using NPOI.SS.UserModel;



using NPOI.SS.Util;
using mooSQL.data;

namespace mooSQL.excel
{
    /// <summary>
    /// Excel�������Ĵ��� �ࡣ
    /// </summary>
    public partial class ExcelBuilder
    {
        /// <summary>
        /// ExcelBuilder type.
        /// </summary>
        /// <summary>
        /// ExcelBuilder type.
        /// </summary>
        public ExcelBuilder()
        {

        }

        /*
        private void getCellFormatByDataType(string str)
        {
            //�����г��˳��õ��ֶ�����  
            switch (str)
            {
                
                case stylexls.ͷ:
                    // cellStyle.FillPattern = FillPatternType.LEAST_DOTS;  
                    cellStyle.SetFont(font12);
                    break;
                case stylexls.ʱ��:
                    IDataFormat datastyle = wb.CreateDataFormat();

                    cellStyle.DataFormat = datastyle.GetFormat("yyyy/mm/dd");
                    cellStyle.SetFont(font);
                    break;
                case stylexls.����:
                    cellStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
                    cellStyle.SetFont(font);
                    break;
                case stylexls.Ǯ:
                    IDataFormat format = wb.CreateDataFormat();
                    cellStyle.DataFormat = format.GetFormat("��#,##0");
                    cellStyle.SetFont(font);
                    break;
                case stylexls.url:
                    fontcolorblue.Underline = 1;
                    cellStyle.SetFont(fontcolorblue);
                    break;
                case stylexls.�ٷֱ�:
                    cellStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00%");
                    cellStyle.SetFont(font);
                    break;
                case stylexls.���Ĵ�д:
                    IDataFormat format1 = wb.CreateDataFormat();
                    cellStyle.DataFormat = format1.GetFormat("[DbNum2][$-804]0");
                    cellStyle.SetFont(font);
                    break;
                case stylexls.��ѧ������:
                    cellStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00E+00");
                    cellStyle.SetFont(font);
                    break;
                case stylexls.Ĭ��:
                    cellStyle.SetFont(font);
                    break;
            }
            //return cellStyle;
        }*/
        /// <summary>
        /// colInfo
        /// </summary>
        /// <summary>
        /// colInfo
        /// </summary>
        public class colInfo
        {
 
        }
        /// <summary>
        /// Gets getTargetByCols.
        /// </summary>

        public ExportTarget getTargetByCols(string excelVersion, List<ExportCol> cols, string fromPart, int position, DBInstance db)
        {
            if (cols.Count == 0) return null;
            var colstr = new StringBuilder();
            for (int i = 0; i < cols.Count; i++)
            //   foreach (var kv in cols)
            {
                var key = cols[i].key;
                if (string.IsNullOrWhiteSpace(key)) key = "col" + i;
                cols[i].key = key;
                if (colstr.Length > 0) { colstr.Append(","); }

                colstr.Append(string.Format("{0} as {1}", cols[i].select, key));
            }
            var cksql = string.Format("select {0} from {1}", colstr, fromPart);
            var dt = db.ExeQuery(cksql, new mooSQL.data.Paras());
            for (int i = 0; i < cols.Count; i++)
            //foreach (var kv in cols)
            {
                var col = dt.Columns[i];
                if (col != null)
                {
                    col.Caption = cols[i].caption;
                }
            }
            var res = new ExportTarget(dt, excelVersion);
            res.colInfos = cols;
            return res;
        }
        /// <summary>
        /// DataTable������Excel�ļ�
        /// </summary>
        public void saveToFile(ExportTarget tar)
        {
            using (MemoryStream ms = getExportStream(tar))
            {
                using (FileStream fs = new FileStream(tar.savePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        }

        /// <summary>
        /// 将导出目标写入 Excel 并返回 MemoryStream。
        /// </summary>
        /// <param name="tar">导出目标。</param>
        public MemoryStream getExportStream(ExportTarget tar)
        {
            if (tar.book == null || !tar.isInited)
            {
                tar.init();
            }
            var workbook = tar.book;
            var sheet = workbook.CreateSheet();

            #region �һ��ļ� ������Ϣ
            if (tar.excelVersion != "2007")
            {
                var realbook = workbook as HSSFWorkbook;
                if (realbook != null)
                {
                    DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                    dsi.Company = "NPOI";
                    realbook.DocumentSummaryInformation = dsi;

                    SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
                    si.Author = tar.Author; //���xls�ļ�������Ϣ
                    si.ApplicationName = tar.ApplicationName; //���xls�ļ�����������Ϣ
                    si.LastAuthor = tar.LastAuthor; //���xls�ļ���󱣴�����Ϣ
                    si.Comments = tar.AuthorInfo; //���xls�ļ�������Ϣ
                    si.Title = tar.Title; //���xls�ļ�������Ϣ

                    si.Subject = tar.Theme;//����ļ�������Ϣ
                    si.CreateDateTime = DateTime.Now;
                    realbook.SummaryInformation = si;
                    workbook = realbook;
                }

            }

            #endregion

            var tdStyle = workbook.CreateCellStyle();
            tdStyle.CloneStyleFrom(tar.colValueStyle);

            var dateStyle = workbook.CreateCellStyle();
            var format = workbook.CreateDataFormat();
            dateStyle.CloneStyleFrom(tar.colValueStyle);
            dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
            var dtSource = tar.data;

            int rowIndex = 0;
            foreach (DataRow row in dtSource.Rows)
            {
                #region �½��������ͷ�������ͷ����ʽ
                if ((rowIndex == 65535 && tar.excelVersion != "2007") || rowIndex == 0)
                {
                    if (rowIndex != 0)
                    {
                        sheet = workbook.CreateSheet();
                        rowIndex = 0;
                    }

                    #region ��ͷ����ʽ
                    if (tar.hasTitle)
                    {
                        var headerRow = sheet.CreateRow(rowIndex);
                        headerRow.HeightInPoints = tar.titleHeight;
                        headerRow.CreateCell(0).SetCellValue(tar.Title);

                        headerRow.GetCell(0).CellStyle = tar.titleStyle;
                        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, dtSource.Columns.Count - 1));
                        //headerRow.Dispose();
                        rowIndex++;
                    }
                    #endregion


                    #region ��ͷ����ʽ
                    {
                        var headerRow = sheet.CreateRow(rowIndex);
                        headerRow.HeightInPoints = tar.colnameHeight;
                        for (int c = 0; c < tar.colInfos.Count; c++)
                        {
                            var col = tar.colInfos[c];
                            var cell = headerRow.CreateCell(c);
                            cell.SetCellValue(col.caption);
                            cell.CellStyle = tar.colNameStyle;

                            //�����п�
                            int widtd = 250;
                            if (col.width < widtd)
                            {
                                widtd = col.width + 2;
                            }
                            sheet.SetColumnWidth(c, widtd * 256);
                            if (col.show == false)
                            {
                                sheet.SetColumnHidden(c, true);
                            }
                        }
                        //headerRow.Dispose();
                        rowIndex++;
                    }
                    #endregion

                    //rowIndex = 2;
                }
                #endregion


                #region �������
                var dataRow = sheet.CreateRow(rowIndex);
                dataRow.HeightInPoints = tar.colvalueHeight;
                //foreach (DataColumn column in dtSource.Columns)
                for (int c = 0; c < tar.colInfos.Count; c++)
                {
                    var col = tar.colInfos[c];
                    var newCell = dataRow.CreateCell(c);
                    newCell.CellStyle = col.style;

                    string drValue = row[col.key].ToString();
                    newCell.SetCellValue(drValue);
                    var typename = col.typeName;
                    if (string.IsNullOrWhiteSpace(typename))
                    {
                        typename = tar.data.Columns[col.key].DataType.Name;
                    }
                    //var mycellstyle = workbook.CreateCellStyle();
                    //mycellstyle.CloneStyleFrom(tar.colValueStyle);
                    //switch (type.ToString())
                    switch (typename)
                    {
                        case "Guid":
                        case "String"://�ַ�������
                            //newCell.SetCellValue(drValue);
                            newCell.SetCellType(CellType.String);
                            break;
                        case "DateTime"://��������
                            DateTime dateV;
                            if (DateTime.TryParse(drValue, out dateV))
                            {
                                newCell.SetCellValue(dateV);
                            }
                            newCell.CellStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
                            break;
                        case "Boolean"://������
                            bool boolV = false;
                            bool.TryParse(drValue, out boolV);
                            newCell.SetCellValue(boolV);
                            newCell.SetCellType(CellType.Boolean);
                            break;
                        case "Int16"://����
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
                        case "Decimal"://������
                        case "Double":
                            double doubV = 0;
                            double.TryParse(drValue, out doubV);
                            //newCell.SetCellValue(doubV);

                            //newCell.CellStyle.DataFormat = format.GetFormat("0.00");
                            newCell.SetCellType(CellType.String);
                            break;
                        case "DBNull"://��ֵ����
                            newCell.SetCellValue("");

                            break;
                        default:
                            newCell.SetCellValue(drValue);
                            newCell.CellStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("@");
                            newCell.SetCellType(CellType.String);

                            break;
                    }
                }
                #endregion
                /*
                  HSSFSheet lo_sheet = (HSSFSheet)lo_workbook.CreateSheet("sheet");

         HSSFCellStyle lo_Style = (HSSFCellStyle)lo_workbook.CreateCellStyle();
         lo_Style.DataFormat = HSSFDataFormat.GetBuiltinFormat("@");

         ���⸽��Դ���е�ע�Ͳ��֣�����HSSFDataFormat������

          0, "General"
           1, "0"
           2, "0.00"
           3, "#,##0"
           4, "#,##0.00"
           5, "($#,##0_);($#,##0)"
           6, "($#,##0_);[Red]($#,##0)"
           7, "($#,##0.00);($#,##0.00)"
           8, "($#,##0.00_);[Red]($#,##0.00)"
           9, "0%"
           0xa, "0.00%"
           0xb, "0.00E+00"
           0xc, "# ?/?"
           0xd, "# ??/??"
           0xe, "m/d/yy"
           0xf, "d-mmm-yy"
           0x10, "d-mmm"
           0x11, "mmm-yy"
           0x12, "h:mm AM/PM"
           0x13, "h:mm:ss AM/PM"
           0x14, "h:mm"
           0x15, "h:mm:ss"
           0x16, "m/d/yy h:mm"
   
            0x17 - 0x24 reserved for international and Undocumented
           0x25, "(#,##0_);(#,##0)"
           0x26, "(#,##0_);[Red](#,##0)"
           0x27, "(#,##0.00_);(#,##0.00)"
           0x28, "(#,##0.00_);[Red](#,##0.00)"
           0x29, "_(///#,##0_);_(///(#,##0);_(/// \"-\"_);_(@_)"
           0x2a, "_($///#,##0_);_($///(#,##0);_($/// \"-\"_);_(@_)"
           0x2b, "_(///#,##0.00_);_(///(#,##0.00);_(///\"-\"??_);_(@_)"
           0x2c, "_($///#,##0.00_);_($///(#,##0.00);_($///\"-\"??_);_(@_)"
           0x2d, "mm:ss"
           0x2e, "[h]:mm:ss"
           0x2f, "mm:ss.0"
           0x30, "##0.0E+0"
           0x31, "@" - This Is text format.
           0x31  "text" - Alias for "@"
                 */
                rowIndex++;
            }
            using (NpoiMemoryStream ms = new NpoiMemoryStream())
            {
                ms.AllowClose = false;
                workbook.Write(ms);
                ms.Flush();
                ms.Position = 0;
                ms.AllowClose = true;

                //sheet.Dispose();
                //workbook.Dispose();//һ��ֻ��д��һ����OK�ˣ�����������ͷ�������Դ������ǰ�汾����������ֻ�ͷ�sheet
                return ms;
            }
        }
        /// <summary>
        /// NpoiMemoryStream
        /// </summary>

        public class NpoiMemoryStream : MemoryStream
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExcelBuilder"/> class.
            /// </summary>
            /// <summary>
            /// Initializes a new instance of the <see cref="ExcelBuilder"/> class.
            /// </summary>
            public NpoiMemoryStream()
            {
                AllowClose = true;
            }
            /// <summary>
            /// Gets or sets AllowClose.
            /// </summary>

            public bool AllowClose { get; set; }
/// <summary>
/// Close
/// </summary>
 

            public override void Close()
            {
                if (AllowClose)
                    base.Close();
            }
        }


        /// <summary>��ȡexcel
        /// Ĭ�ϵ�һ��Ϊ��ͷ
        /// </summary>
        /// <param name="strFileName">excel�ĵ�·��</param>
        /// <returns></returns>
        public static DataTable Import(string strFileName)
        {
            DataTable dt = new DataTable();

            HSSFWorkbook hssfworkbook;
            using (FileStream file = new FileStream(strFileName, FileMode.Open, FileAccess.Read))
            {
                hssfworkbook = new HSSFWorkbook(file);
            }
            var sheet = hssfworkbook.GetSheetAt(0);
            System.Collections.IEnumerator rows = sheet.GetRowEnumerator();

            var headerRow = sheet.GetRow(0);
            int cellCount = headerRow.LastCellNum;

            for (int j = 0; j < cellCount; j++)
            {
                var cell = headerRow.GetCell(j);
                dt.Columns.Add(cell.ToString());
            }

            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                DataRow dataRow = dt.NewRow();

                for (int j = row.FirstCellNum; j < cellCount; j++)
                {
                    if (row.GetCell(j) != null)
                        dataRow[j] = row.GetCell(j).ToString();
                }

                dt.Rows.Add(dataRow);
            }
            return dt;
        }

    }
}
