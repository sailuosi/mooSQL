using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;


namespace mooSQL.excel
{
    /// <summary>
    /// excel导出列的配置
    /// </summary>
    public class ExportCol
    {
        public string colname;
        public string caption;
        public string codeTableID = "";
        public int width;
        public bool fixedWidth = false;
        public string typeName;
        public string key;//用作查询语句的as.即作为查出数据的列名
        public string select;//用来查询的select列语句部分。
        public ICellStyle style;

        public Dictionary<string,string> valueMap = new Dictionary<string,string>();

        public Func<DataRow, string> loadValue;
        public bool isTableColumn = true;
        public string value;

        /// <summary>
        /// 是否显示列
        /// </summary>
        public bool show = true;



        public ExportCol setShow(bool isShow)
        {
            this.show = isShow;
            return this;
        }
        public ExportCol(string col)
        {
            this.colname = col;
            this.caption = col;
            this.select = col;
        }
        public ExportCol(string col, string title)
        {
            this.colname = col;
            this.caption = title;
            this.select = this.colname;
        }
        public ExportCol(string col, string title, string codeTableID)
        {
            this.colname = col;
            this.caption = title;
            this.codeTableID = codeTableID;
            this.select = string.Format("(select v.CodeName from CodeValue v where v.CodeTableID='{0}' and v.CodeID={1}) ", codeTableID, col);

        }
        public ExportCol(string col, string title, int width)
        {
            this.colname = col;
            this.caption = title;
            this.width = width;
        }
        public ExportCol setWidth(int wd)
        {
            if (wd > 0 && wd < 240)
            {
                this.width = wd;
                this.fixedWidth = true;
            }
            return this;
        }

        public string fieldName{
            get {
                if (!string.IsNullOrWhiteSpace(this.colname)) { 
                    return this.colname;
                }
                return this.key;
            }
        }
        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public string getValue(DataRow row)
        {
            if(this.loadValue != null)
            {
                return this.loadValue(row);
            }

            string val = null;

            if (!string.IsNullOrWhiteSpace(this.fieldName) && row.Table.Columns.Contains(fieldName)) { 
                val= row[fieldName].ToString();
            }
            if(this.valueMap != null && valueMap.ContainsKey(val))
            {
                return valueMap[val];
            }
            return val;

        }
    }
}
