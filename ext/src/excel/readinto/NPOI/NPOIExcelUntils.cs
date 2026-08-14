

using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static class NPOIExcelUntils
    {
        
        /// <summary>
        /// 创建公式计算器
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static object getCellValue(ICell cell)
        {
            var value = new object();
            //CellType(Unknown = -1,Numeric = 0,String = 1,Formula = 2,Blank = 3,Boolean = 4,Error = 5,)
            switch (cell.CellType)
            {
                case CellType.Formula:
                    cell.SetCellType(CellType.String);
                    value = cell.StringCellValue;
                    break;
                case CellType.Blank:
                    value = "";
                    break;
                case CellType.Numeric:
                    //short format = cell.CellStyle.DataFormat;
                    bool isDate = DateUtil.IsCellDateFormatted(cell);
                    var cellformat = cell.CellStyle.GetDataFormatString();
                    if (isDate)
                    {
                        value = cell.DateCellValue;
                    }
                    else
                    {
                        value = cell.NumericCellValue;
                    }
                    //string formatString = cell.CellStyle.GetDataFormatString();
                    //string value = cell.NumericCellValue.ToString(formatString);
                    //对时间格式（2015.12.5、2015/12/5、2015-12-5等）的处理
                    /*
                    if (format == 14 || format == 31 || format == 57 || format == 58)
                        value = cell.DateCellValue;
                    else
                        value = cell.NumericCellValue;*/
                    break;
                case CellType.String:
                    var val = cell.StringCellValue;
                    if (val.Contains("'"))
                    {
                        val = val.Replace("'", "\"");
                    }
                    value = val.Trim();
                    break;
                case CellType.Boolean:
                    value = cell.BooleanCellValue;
                    break;
                default:
                    value = "";
                    break;
            }
            return value;
        }
        private static string GetCellValueFromFormula(CellValue formulaValue)
        {
            switch (formulaValue.CellType)
            {
                case CellType.String: return formulaValue.StringValue;
                case CellType.Numeric: return formulaValue.NumberValue.ToString();
                case CellType.Boolean: return formulaValue.BooleanValue.ToString();
                case CellType.Error: return $"#公式错误: {formulaValue.ErrorValue}";
                default: return string.Empty;
            }
        }
        public static XCell CellToXCell(ICell cell, IFormulaEvaluator evaluator)
        {
            var xcell = new XCell();
            xcell.type = "s";
            xcell.columnIndex= cell.ColumnIndex;
            xcell.rowIndex= cell.RowIndex;
            //CellType(Unknown = -1,Numeric = 0,String = 1,Formula = 2,Blank = 3,Boolean = 4,Error = 5,)
            switch (cell.CellType)
            {
                case CellType.Formula:
                    try
                    {
                        CellValue formulaValue = evaluator.Evaluate(cell);
                        var v= GetCellValueFromFormula(formulaValue);
                        xcell.value = v;
                        xcell.typeValue = v;
                    }
                    catch (Exception ex)
                    {
                        cell.SetCellType(CellType.String);
                        xcell.value = cell.StringCellValue;
                    }

                    break;
                case CellType.Blank:
                    xcell.value = "";
                    break;
                case CellType.Numeric:
                    //short format = cell.CellStyle.DataFormat;
                    bool isDate = DateUtil.IsCellDateFormatted(cell);
                    var cellformat = cell.CellStyle.GetDataFormatString();
                    if (isDate)
                    {
                        xcell.value = cell.DateCellValue.ToString();
                        xcell.typeValue = cell.DateCellValue;
                    }
                    else
                    {
                        xcell.value = cell.NumericCellValue.ToString();
                        xcell.typeValue = cell.NumericCellValue;
                    }
                    //string formatString = cell.CellStyle.GetDataFormatString();
                    //string value = cell.NumericCellValue.ToString(formatString);
                    //对时间格式（2015.12.5、2015/12/5、2015-12-5等）的处理
                    /*
                    if (format == 14 || format == 31 || format == 57 || format == 58)
                        value = cell.DateCellValue;
                    else
                        value = cell.NumericCellValue;*/
                    break;
                case CellType.String:
                    var val = cell.StringCellValue;
                    xcell.value = val;
                    xcell.typeValue = val;
                    break;
                case CellType.Boolean:
                    xcell.value = cell.BooleanCellValue.ToString();
                    xcell.typeValue = cell.BooleanCellValue;
                    break;
                default:
                    break;
            }
            return xcell;
        }
        public static bool IsMergeCell(ISheet sheet, int rowIndex, int colIndex, out ICell cell)
        { //out Point start, out Point end
            bool result = false;
            cell = null;
            //start = new Point(0, 0);
            //end = new Point(0, 0);
            if ((rowIndex < 0) || (colIndex < 0)) return result;
            cell = sheet.GetRow(rowIndex).GetCell(colIndex);
            int regionsCount = sheet.NumMergedRegions;
            for (int i = 0; i < regionsCount; i++)
            {
                var range = sheet.GetMergedRegion(i);
                //sheet.IsMergedRegion(range); 
                if (rowIndex >= range.FirstRow && rowIndex <= range.LastRow && colIndex >= range.FirstColumn && colIndex <= range.LastColumn)
                {
                    var rrow = sheet.GetRow(range.FirstRow);
                    cell = rrow.GetCell(range.FirstColumn);
                    //start = new Point(range.FirstRow, range.FirstColumn);
                    //end = new Point(range.LastRow, range.LastColumn);
                    result = true;
                    break;
                }
            }
            return result;
        }

    }
}
