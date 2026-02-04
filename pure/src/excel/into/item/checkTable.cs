using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;





namespace mooSQL.excel.context
{
    /// <summary>
    /// 核验数据存储类。
    /// </summary>
    public class checkTable 
    {
        public checkTable(string name) 
        {
            this.DBName = name;
        }

        public string selectStr;
        public string keyColName;

        public Table option { get; set; }
        public ExcelRead root;
        private DataTable dataTable;
        private DataTable emptyTable;
        public DataColumnCollection allCols
        {
            get
            {
                if(this.emptyTable == null) {
                    emptyTable = DBInstance.ExeQuery(builder.getEmptySelect(DBName));
                }
                return emptyTable.Columns;
            }
        }
        public bool Empty
        {
            get { 
                if(this.dataTable == null|| this.dataTable.Rows.Count==0) {
                    return true;
                }
                return false;
            }
        }
        public DataTable table
        {
            get
            {
                if (dataTable == null) {
                    this.readData();
                }
                return dataTable;
            }
        }
        private DBInstance _mydb = null;

        public DBInstance DBInstance
        {
            get
            {
                if (_mydb == null)
                {
                    _mydb = root.GetDBInstance(option.position);
                }
                return _mydb;
            }
        }

        private SQLBuilder _sqlBuilder;
        public SQLBuilder builder
        {
            get { 
                if(_sqlBuilder == null)
                {
                    _sqlBuilder = new SQLBuilder();
                    _sqlBuilder.setDBInstance(DBInstance);
                }
                return _sqlBuilder;
            }
        }
        
        public string DBName;//数据库表名
        public string caption;//
        public List<string> readCols = new List<string>(); //用来查询的字段集合
                                                           //限制查询范围 where in的列
        public Dictionary<string, WhereInBuilder> whereInFields = new Dictionary<string, WhereInBuilder>();
        public string selectSQL;//查询获取数据的SQL语句
        public string checkWhere = "";
        //public bool canInsert = false;
        //public bool canUpdate = false;
        //public long insertCount = 0;
        //public long updateCount = 0;
        //public int updateSuccessCount = 0;//更新SQL语句分段执行时，使用。

        //public StringBuilder updateSql = new StringBuilder();
        public void readFromConfig(WriteTable tb, ExcelRead father)
        {
            //if (tb.canInsert) canInsert = true;
            //if (tb.canUpdate) canUpdate = true;
            if (string.IsNullOrWhiteSpace(tb.option.caption) == false && caption != DBName) this.caption = tb.option.caption;
            foreach (var c in tb.writeCols)
            {
                addCheckCol(c.Key);
            }
            if (string.IsNullOrWhiteSpace(tb.baseCols) == false)
            {
                var cp = tb.baseCols.Split(',');
                foreach (var c in cp)
                {
                    addCheckCol(c);
                }
            }
            if (tb.option.whereInFields != null)
            {
                //格式 whereIn:[{field:"",src:"col1,col2"}],
                foreach (var kv in tb.option.whereInFields)
                {
                    foreach (var li in kv.Value)
                    {
                        father.addExcelCheckCol(li);
                        this.addWhereInCol(kv.Key, li);
                    }

                }
            }
            else if (string.IsNullOrWhiteSpace(tb.option.repeatWhere) == false)
            {
                var ExcelCKCols = tb.option.repeatWhere.Split(';');
                foreach (var exck in ExcelCKCols)
                {
                    var exckArr = exck.Split('=');
                    if (exckArr.Length > 1)
                    {

                    }
                }
            }

            if (string.IsNullOrWhiteSpace(tb.option.selectSQL) == false)
            {
                selectSQL = tb.option.selectSQL;
            }
            if (string.IsNullOrWhiteSpace(tb.option.baseWhere) == false)
            {
                checkWhere = tb.option.baseWhere;
            }
            if (string.IsNullOrWhiteSpace(this.caption))
            {
                caption = tb.option.DBName;
            }
        }
        public void addCheckCol(string colname)
        {
            if (allCols.Contains(colname) && readCols.Contains(colname) == false) readCols.Add(colname);
        }
        /// <summary>
        /// 添加一个列到要查询的列
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="colKey"></param>
        public void addWhereInCol(string fieldName, string colKey)
        {
            if (string.IsNullOrWhiteSpace(colKey)) return;
            if (allCols.Contains(fieldName))
            {
                if (whereInFields.ContainsKey(fieldName) == false)
                {
                    var tar = new WhereInBuilder();
                    whereInFields.Add(fieldName, tar);
                }

                whereInFields[fieldName].addSrcField(colKey);
                
            }
        }
        /// <summary>
        /// 加载历史数据。
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void readData()
        {
            if (this.option != null) { 
                if(this.option.onLoadData != null)
                {
                    dataTable = option.onLoadData(this);
                    return;
                }
            }
            if (this.DBName == null) { throw new Exception("要查询表表名为空！"); }
            if (string.IsNullOrWhiteSpace(selectSQL))
            {
                var colnames = getSelectPart();
                selectSQL = string.Format("select {0} from {1}", colnames, DBName);
                if (string.IsNullOrWhiteSpace(checkWhere) == false)
                {
                    selectSQL += " where " + checkWhere;
                }
            }
            this.selectStr = selectSQL;
            this.loadData();
        }
        private void loadData() {
            this.dataTable= DBInstance.ExeQuery(selectStr, new data.Paras());
        }
        private string getSelectPart()
        {
            var par = "";
            if (this.option != null)
            {
                if (this.option.selectFields.Count > 0)
                {
                    foreach (var fi in option.selectFields)
                    {
                        if (this.readCols.Contains(fi) == false) readCols.Add(fi);
                    }
                }
            }
            if (readCols.Count == 0)
            {
                readCols.Add(this.keyColName);
            }
            return readCols.JoinNotEmpty( ",");

        }


    }
}
