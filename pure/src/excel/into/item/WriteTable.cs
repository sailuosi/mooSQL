

using System.Collections.Generic;
using System.Data;

using System.Text;



using mooSQL.data;
using mooSQL.utils;

namespace mooSQL.excel.context
{
    /// <summary>
    /// 要写入数据库的表对象
    /// </summary>
    public class WriteTable// :ImportOption.Table
    {
        /// <summary>
        /// 配置对象
        /// </summary>
        public Table option;
        //先为直接读取配置的属性。
        /// <summary>
        /// 是否可以添加数据
        /// </summary>
        public bool canInsert = false;
        /// <summary>
        /// 是否可以更新数据
        /// </summary>
        public bool canUpdate = false;
        //public string writeMode ;//插入的表的模式，包含插入insert/update/all
        /// <summary>
        /// 传入的核验范围列。
        /// </summary>
        public string baseCols;//
        /// <summary>
        /// 为true标识插入，false标识更新，null标识不操作。
        /// </summary>
        public int inserting = 0;
        /// <summary>
        /// 查询的数据源
        /// </summary>
        public checkTable oldData;

        private BulkBase _writor;

        private BatchSQL _batSQL;
        /// <summary>
        /// 批量SQL执行器,用于批量插入更新。替代旧的SQL直接创建模式。
        /// </summary>
        public BatchSQL BatSQL
        {
            get { 
                if (_batSQL == null) {
                    _batSQL = new BatchSQL(root.GetDBInstance(option.position));
                }
                return _batSQL;
            }
        }

        private bool rowStarted = false;
        public SQLBuilder rowKit;
        public int setCount = 0;
        public void set(string key, object value) {
            if (rowStarted == false) {
                StartRow();
            }
            rowKit.set(key, value);
            setCount++;
        }

        public void StartRow() {
            setCount = 0;
            rowKit = BatSQL.newRow();
            rowStarted = true;
            
        }
        public void EndUpdate() {
            if (setCount > 0) { 
                BatSQL.addUpdate();
                rowStarted = false;
            }
        }

        /// <summary>
        /// 写入实体的工作类
        /// </summary>
        public BulkBase bulk
        {
            get {
                if (_writor == null) { 
                    _writor = new BulkBase();
                    _writor.tableName = option.DBName;
                    _writor.DB = root.GetDBInstance(option.position);
                    _writor.getBulkTable();
                }
                return _writor;
            }
        }
        /// <summary>
        /// 写入字段集合。
        /// </summary>
        public Dictionary<string, colInfo> writeCols = new Dictionary<string, colInfo>();
        /// <summary>
        /// 是否自定义范围
        /// </summary>
        public bool customScope = false;
        /// <summary>
        /// 自定义的范围
        /// </summary>
        public IntSection rowScope = new IntSection();
        /// <summary>
        /// 解析后的查重条件语句。
        /// </summary>
        public string parsedRepeatWhere;
        /// <summary>
        /// 解析后的更新条件语句
        /// </summary>
        public string parsedUpdateWhere;
        /// <summary>
        /// 查重时唯一值需要回写的字段信息字符串。
        /// </summary>
        public string[] writeBackInfo;

        /// <summary>
        /// 交叉列格式下的解析特征
        /// </summary>
        public Dictionary<string, dynamicItem> parsedDynamic = new Dictionary<string, dynamicItem>();
        /// <summary>
        /// 记录动态列的索引。
        /// </summary>
        public List<string> dynamicCols = new List<string>();//
        /// <summary>
        /// 更新执行器
        /// </summary>
        public StringBuilder updateSQL = new StringBuilder();
        /// <summary>
        /// SQL参数体
        /// </summary>
        public Paras para = new Paras();
        /// <summary>
        /// 行处理中正在添加的数据行
        /// </summary>
        public DataRow addingRow;
        //写入环境中的局部变量
        /// <summary>
        /// 写入工作对象指针
        /// </summary>
        public ExcelRead root;
        /// <summary>
        /// 存储excel数据的dataTable当前循环的row
        /// </summary>
        public rowInfo srcRow;
        /// <summary>
        /// 存储excel数据的dataTable当前行记录指针
        /// </summary>
        //public int srcIndex;
        /// <summary>
        /// 动态列循环时，当前动态列的列循环位置。
        /// </summary>
        public int dynamicIndex;
        /// <summary>
        /// 当前写入表在全局的索引
        /// </summary>
        public int tableIndex;
        /// <summary>
        /// 主键列信息
        /// </summary>
        public colInfo keyColInfo;
        /// <summary>
        /// 查重得到的结果。
        /// </summary>
        public DataRow[] checkResult;
        /// <summary>
        /// 已添加的记录，存储writewhere的解析结果值
        /// </summary>
        public Dictionary<string, string> addedIds = new Dictionary<string, string>();//
        /// <summary>
        /// 记录写入时的行循环期间查重的where语句。
        /// </summary>
        public string checkingWhere = "";//
        /// <summary>
        /// 更新计数
        /// </summary>
        public int updateCount = 0;
        /// <summary>
        /// 插入计数
        /// </summary>
        public int insertCount = 0;
        /// <summary>
        /// 是否使用表来进行数据的更新，默认不启用，使用SQL语句
        /// </summary>
        public bool doUpdate = false;

        public Dictionary<string, string> updatekv = new Dictionary<string, string>();

        private DBInstance _mydb=null;
        /// <summary>
        /// 数据库功能体。
        /// </summary>
        public DBInstance DBInstance
        {
            get {
                if (_mydb == null) {
                    _mydb = root.GetDBInstance(option.position);
                }
                return _mydb;
            }
        }
        /// <summary>
        /// 数据库写入表
        /// </summary>
        public WriteTable() { }
        /// <summary>
        /// 数据库写入表
        /// </summary>
        /// <param name="name"></param>
        public WriteTable(string name)
        {
            //this.key = name;
            //this.name = name;
            this.tableIndex = -1;
            //this.baseWhere = "";
        }
        /// <summary>
        /// 基于配置创建写入表
        /// </summary>
        /// <param name="opt"></param>
        public WriteTable(Table opt)
        {
            this.option = opt;
            
        }



        /// <summary>
        /// 核验、解析表配置
        /// </summary>
        /// <param name="father"></param>
        public void checkConfig(ExcelRead father)
        {
            //核验主键列信息
            //key是作为写入表的唯一索引。
            if (string.IsNullOrWhiteSpace(option.key)) option.key = option.name + father.Writelist.Count;
            if (string.IsNullOrWhiteSpace(option.DBName)) option.DBName = option.name;
            if (string.IsNullOrWhiteSpace(option.caption)) option.caption = option.name;
            if (string.IsNullOrWhiteSpace(option.keyCol)) option.keyCol = option.DBName + "OID";
            //解析数据范围
            if (string.IsNullOrWhiteSpace(option.dataRowNum) == false)
            {
                this.customScope = true;
                this.rowScope.readConfig(option.dataRowNum);
            }
            bool keyGoted = false;
            foreach (var tar in option.KVs)
            {
                //根据字段名，依次读入各属性。所有的列，必须包含key属性
                var col = new colInfo(); // father.tool.AutoCopy<ImportOption.Column, colInfo>(tar);
                col.option = tar;
                col.root = this.root;
                col.table = this;
                col.chekConfig();
                col.option.isField = true;
                col.colType = tar.colType;
                if (father.isValid(col.key) == false)
                {   //为防止列信息重复的问题。增加计数作为名称。
                    col.key = this.option.key + "_" + col.field + father.context.valueCollection.colMap.Count;
                }

                col.ID = col.key;
                col.option.tableName = option.key;
                if (string.IsNullOrWhiteSpace(col.field) == false)
                {
                    if (string.IsNullOrWhiteSpace(col.caption))
                    {
                        col.caption = col.field;
                    }
                    writeCols[col.field] = col;

                }
                father.context.valueCollection.addCol(col.ID, col);
                if (col.type == columnType.dynamic)
                {
                    father.dynamicCols.AddNotRepeat( col.ID);
                    father.addFocusCol(col);
                    this.option.dynamic = true;
                }
                //核查是不是主键列
                if (!keyGoted)
                {
                    if (tar.key == this.option.keyCol || tar.field == this.option.keyCol)
                    {
                        this.keyColInfo = col;
                        keyGoted = true;
                        col.isPrimaryKey = true;
                        col.writeLocked = true;
                    }
                }
            }
            //如果主键未为定义，不在已定义的集合中，此时自动创建一个。
            if (keyGoted == false)
            {
                var col = new colInfo(); // father.tool.AutoCopy<ImportOption.Column, colInfo>(tar);
                                         //col.option = tar;
                col.root = this.root;
                col.table = this;
                col.option = new Column(root.context.option);
                col.option.field = option.keyCol;
                col.mode = writeMode.insert;
                col.type = columnType.function;
                col.value = "newid";
                col.option.isField = true;

                col.key = this.option.key + "_" + option.keyCol + father.context.valueCollection.colMap.Count;

                col.ID = col.key;
                col.option.tableName = option.key;

                col.caption = "主键" + option.keyCol;

                writeCols[col.field] = col;
                father.context.valueCollection.addCol(col.ID, col);

                this.keyColInfo = col;
                keyGoted = true;
                col.isPrimaryKey = true;
                col.writeLocked = true;

            }

            //检查读写权限
            if (option.mode == writeMode.none)
            {
                option.mode = father.context.option.mode;
            }
            if (option.mode == writeMode.write)
            {
                canInsert = true;
                canUpdate = true;
            }
            else if (option.mode == writeMode.insert)
            {
                canInsert = true;
            }
            else if (option.mode == writeMode.update)
            {
                canUpdate = true;
            }

        }
        /// <summary>
        /// 为写入表添加一个写入列
        /// </summary>
        /// <param name="col"></param>
        public void addCol(colInfo col)
        {
            writeCols[col.field] = col;
            if (col.type == columnType.dynamic)
            {
                if (dynamicCols.Contains(col.key) == false) dynamicCols.Add(col.key);
                if (option.dynamic != true) option.dynamic = true;
            }
            if (col.dynamic)
            {//所有写入列涉及到动态列的表，必须指定为动态列环境下写入。
                if (option.dynamic != true) option.dynamic = true;
            }
        }
        /// <summary>
        /// 根据字段名名，获取key
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string getFieldKey(string fieldName)
        {
            var res = "";
            foreach (var kv in writeCols)
            {
                if (kv.Value.field == fieldName)
                {
                    return kv.Value.key;
                }
            }
            return res;
        }
        /// <summary>
        /// 使用一组过滤SQL条件，获得数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sqlWhere"></param>
        /// <returns></returns>
        public DataRow[] getCheckRows(string tableName, string sqlWhere)
        {
            DataRow[] rows;
            int errcout;
            string wherestr = root.context.valueCollection.formatFreeSQLValue(sqlWhere, out errcout);
            if (wherestr == "" || errcout > 0)
            {
                return null;
            }
            if (root.context.option.checkMode == "local")
            {
                DataTable tardt = root.getBaseDataTable(tableName);
                rows = tardt.Select(wherestr);
            }
            else
            {
                string wherepart = wherestr;
                if (! root.baseTable.ContainsKey(tableName))
                {
                    return null;
                }
                var tb = root.baseTable[tableName];
                if (tb.checkWhere != "")
                {
                    wherepart += wherepart == "" ? "" : " and ";
                    wherepart += tb.checkWhere;
                }
                if (wherepart != "")
                {
                    wherepart = " where " + wherepart;
                }
                string colnames = tb.readCols.JoinNotEmpty( ",");
                string findSQLs = string.Format("select {0} from {1} {2}", colnames, tb.DBName, wherepart);
                DataTable temptdt = DBInstance.ExeQuery(findSQLs,new Paras());
                rows = temptdt.Select();
            }
            return rows;
        }
        /// <summary>
        /// 提交变更到数据库，执行插入或更新。
        /// </summary>
        public void save() {

            if (option != null && option.onBeforeSave != null)
            {
                var re = option.onBeforeSave(this);
                if (re == false) return ;
            }
            if (canInsert)
            {

                var bk = DBInstance.dialect.BulkInsert(bulk);
                insertCount += bk;

                
            }
            if (canUpdate)
            {
                ployUpdteSQL();
                updateCount +=this.BatSQL.exeNonQuery();
            }
        }
        /// <summary>
        /// 执行更新的SQL
        /// </summary>
        public void ployUpdteSQL()
        {
            if (canUpdate == false || updateSQL.Length == 0) return;
            updateCount += DBInstance.ExeNonQuery(updateSQL.ToString(),para);
            updateSQL.Clear();
        }

        /// <summary>
        /// 清理正在添加或修改的当前行数据
        /// </summary>
        public void clearRowData()
        {
            this.addingRow = null;
            this.updatekv.Clear();
        }
    }
}
