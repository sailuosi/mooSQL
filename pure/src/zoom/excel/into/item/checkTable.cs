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
        /// <summary>
        /// 指定数据库表名，创建用于导入前查重/核验的数据上下文。
        /// </summary>
        /// <param name="name">数据库中的表名。</param>
        public checkTable(string name) 
        {
            this.DBName = name;
        }

        /// <summary>当前实际执行的查询 SQL 文本（可能由配置或自动生成）。</summary>
        public string selectStr;
        /// <summary>作为业务主键或唯一匹配的列名。</summary>
        public string keyColName;

        /// <summary>关联的表级导入配置。</summary>
        public Table option { get; set; }
        /// <summary>所属导入会话（用于取库连接、格式化等）。</summary>
        public ExcelRead root;
        private DataTable dataTable;
        private DataTable emptyTable;
        /// <summary>目标表在数据库中的全部列元数据（通过空结果查询取得）。</summary>
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
        /// <summary>是否尚未加载到任何核验数据行。</summary>
        public bool Empty
        {
            get { 
                if(this.dataTable == null|| this.dataTable.Rows.Count==0) {
                    return true;
                }
                return false;
            }
        }
        /// <summary>已加载的核验结果集；首次访问时按需查询数据库。</summary>
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

        /// <summary>根据配置解析得到的数据库访问实例。</summary>
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
        /// <summary>用于拼装核验查询的 SQL 构造器（已绑定 <see cref="DBInstance"/>）。</summary>
        public SQLBuilder builder
        {
            get { 
                if(_sqlBuilder == null)
                {
                    _sqlBuilder = DBInstance.useSQL();
                }
                return _sqlBuilder;
            }
        }
        
        /// <summary>数据库表名。</summary>
        public string DBName;//数据库表名
        /// <summary>表的中文标题或展示名（可与物理表名不同）。</summary>
        public string caption;//
        /// <summary>参与 SELECT 的字段集合，用于缩小核验查询列。</summary>
        public List<string> readCols = new List<string>(); //用来查询的字段集合
                                                           //限制查询范围 where in的列
        /// <summary>按数据库字段名组织的 WHERE IN 取值构建器（键为字段名）。</summary>
        public Dictionary<string, WhereInBuilder> whereInFields = new Dictionary<string, WhereInBuilder>();
        /// <summary>自定义查询 SQL；若为空则按 <see cref="readCols"/> 与 <see cref="DBName"/> 自动生成。</summary>
        public string selectSQL;//查询获取数据的SQL语句
        /// <summary>附加在核验查询上的固定 WHERE 片段（不含 where 关键字）。</summary>
        public string checkWhere = "";
        //public bool canInsert = false;
        //public bool canUpdate = false;
        //public long insertCount = 0;
        //public long updateCount = 0;
        //public int updateSuccessCount = 0;//更新SQL语句分段执行时，使用。

        //public StringBuilder updateSql = new StringBuilder();
        /// <summary>
        /// 从写入表配置与导入上下文读取查重列、自定义 SQL、WHERE IN 映射等，填充本对象。
        /// </summary>
        /// <param name="tb">当前工作写入表。</param>
        /// <param name="father">导入主对象。</param>
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
        /// <summary>
        /// 将列名加入待 SELECT 的核验列集合（须为表中真实列）。
        /// </summary>
        /// <param name="colname">数据库列名。</param>
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
