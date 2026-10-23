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
        /// <summary>
        /// colname
        /// </summary>
        /// <summary>
        /// colname
        /// </summary>
        public string colname;
        /// <summary>
        /// caption
        /// </summary>
        /// <summary>
        /// caption
        /// </summary>
        public string caption;
        /// <summary>
        /// codeTableID
        /// </summary>
        /// <summary>
        /// codeTableID
        /// </summary>
        public string codeTableID = "";
        /// <summary>
        /// width
        /// </summary>
        /// <summary>
        /// width
        /// </summary>
        public int width;
        /// <summary>
        /// fixedWidth
        /// </summary>
        /// <summary>
        /// fixedWidth
        /// </summary>
        public bool fixedWidth = false;
        /// <summary>
        /// typeName
        /// </summary>
        /// <summary>
        /// typeName
        /// </summary>
        public string typeName;
        /// <summary>
        /// key
        /// </summary>
        /// <summary>
        /// key
        /// </summary>
        public string key;//用作查询语句的as.即作为查出数据的列名
        /// <summary>
        /// select
        /// </summary>
        /// <summary>
        /// select
        /// </summary>
        public string select;//用来查询的select列语句部分。
        /// <summary>
        /// style
        /// </summary>
        /// <summary>
        /// style
        /// </summary>
        public ICellStyle style;
        /// <summary>
        /// valueMap
        /// </summary>

        public Dictionary<string,string> valueMap = new Dictionary<string,string>();
        /// <summary>
        /// loadValue
        /// </summary>

        public Func<DataRow, string> loadValue;
        /// <summary>
        /// isTableColumn
        /// </summary>
        /// <summary>
        /// isTableColumn
        /// </summary>
        public bool isTableColumn = true;
        /// <summary>
        /// value
        /// </summary>
        /// <summary>
        /// value
        /// </summary>
        public string value;

        /// <summary>
        /// 是否显示列
        /// </summary>
        public bool show = true;
        /// <summary>
        /// setShow
        /// </summary>



        public ExportCol setShow(bool isShow)
        {
            this.show = isShow;
            return this;
        }
        /// <summary>
        /// ExportCol type.
        /// </summary>
        /// <summary>
        /// ExportCol type.
        /// </summary>
        public ExportCol(string col)
        {
            this.colname = col;
            this.caption = col;
            this.select = col;
        }
        /// <summary>
        /// ExportCol type.
        /// </summary>
        /// <summary>
        /// ExportCol type.
        /// </summary>
        public ExportCol(string col, string title)
        {
            this.colname = col;
            this.caption = title;
            this.select = this.colname;
        }
        /// <summary>
        /// ExportCol type.
        /// </summary>
        /// <summary>
        /// ExportCol type.
        /// </summary>
        public ExportCol(string col, string title, string codeTableID)
        {
            this.colname = col;
            this.caption = title;
            this.codeTableID = codeTableID;
            this.select = string.Format("(select v.CodeName from CodeValue v where v.CodeTableID='{0}' and v.CodeID={1}) ", codeTableID, col);

        }
        /// <summary>
        /// ExportCol type.
        /// </summary>
        /// <summary>
        /// ExportCol type.
        /// </summary>
        public ExportCol(string col, string title, int width)
        {
            this.colname = col;
            this.caption = title;
            this.width = width;
        }
        /// <summary>
        /// setWidth
        /// </summary>
        /// <summary>
        /// setWidth
        /// </summary>
        public ExportCol setWidth(int wd)
        {
            if (wd > 0 && wd < 240)
            {
                this.width = wd;
                this.fixedWidth = true;
            }
            return this;
        }
        /// <summary>
        /// fieldName
        /// </summary>

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
