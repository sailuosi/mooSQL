
using mooSQL.utils;

using System;
using System.Collections.Generic;
using System.Data;

namespace mooSQL.excel.context
{
    /// <summary>
    /// 配置的表
    /// </summary>
    public partial class Table
    {
        /// <summary>
        /// 
        /// </summary>
        public Table()
        {

        }
        /// <summary>
        /// 基于全局配置的表配置
        /// </summary>
        /// <param name="root"></param>
        public Table(ImportOption root)
        {
            this.root = root;
        }
        /// <summary>
        /// 全局配置
        /// </summary>
        public ImportOption root;
        /// <summary>
        /// 表名
        /// </summary>
        public string name;
        /// <summary>
        /// 表key
        /// </summary>
        public string key;
        /// <summary>
        /// 表的数据库连接位
        /// </summary>
        public int position = 0;
        /// <summary>
        /// 中文标题
        /// </summary>
        public string caption;
        /// <summary>
        /// 数据库中的表名
        /// </summary>
        public string DBName;
        /// <summary>
        /// 自定义表初始化查询时的SQL语句，默认自动根据列信息进行构建。一般不用设置。
        /// </summary>
        public string selectSQL;
        /// <summary>
        /// 默认为false 本表是否执行批量更新，与全局参数含义相同。
        /// </summary>
        public bool batchUpdate = false;
        /// <summary>
        /// 写入模式
        /// </summary>
        public writeMode mode;
        /// <summary>
        /// 校验失败的动作
        /// </summary>
        public checkFailAct failPolicy = checkFailAct.self;
        /// <summary>
        /// 包含共享列
        /// </summary>
        public bool useShareCol = true;
        /// <summary>
        /// 主键列
        /// </summary>
        public string keyCol = string.Empty;
        /// <summary>
        /// 必填，查重使用的where条件串。
        /// </summary>
        public string repeatWhere;
        /// <summary>
        /// 执行更新的额外条件字符串
        /// </summary>
        public string updateWhere;
        /// <summary>
        /// 执行更新的校验。使用时，将忽略updateWhere的设置
        /// </summary>
        public Func<WriteTable, DataRow, bool> onCheckUpdate;
        /// <summary>
        /// 查重失败时的提示内容。可用{auto}指代默认的提示。
        /// </summary>
        public string repeatErrTip = "{auto}";
        /// <summary>
        /// 表记录查重的方法，定义该方法时，将不再执行默认的查重方法。
        /// </summary>
        public Func<WriteTable, DataRow[]> onCheckRepeat;
        /// <summary>
        /// 表行数据写入前时刻。返回false时将停止插入动作。
        /// </summary>
        public Func<WriteTable, bool> onBeforeRowAdd;
        /// <summary>
        /// 保存数据前的处理方法，返回false时停止保存
        /// </summary>
        public Func<WriteTable, bool> onBeforeSave;

        public Func<checkTable, DataTable> onLoadData;
        /// <summary>
        /// 表的写入数据范围，默认为全局的数据体范围。
        /// </summary>
        public string dataRowNum;
        /// <summary>
        /// 
        /// </summary>
        public string baseWhere;
        /// <summary>
        /// 查重得到唯一行时，需要对列值库进行数据回写的字段。
        /// </summary>
        public string repeatBackKeys;
        /// <summary>
        /// 是否动态写入表
        /// </summary>
        public bool dynamic = false;
        /// <summary>
        /// 动态时，需要重算的列
        /// </summary>
        public List<string> reComputeCols = new List<string>();
        /// <summary>
        /// 缩小查询旧数据的列范围
        /// </summary>
        public Dictionary<string, List<string>> whereInFields = new Dictionary<string, List<string>>();
        /// <summary>
        /// 对应前端配置的逗号分割的baseCols属性。里面的字段是用来自定义查询表历史数据时，用到的字段。
        /// </summary>
        public List<string> selectFields = new List<string>();
        /// <summary>
        /// 表的列配置
        /// </summary>
        public List<Column> KVs = new List<Column>();
        private myUntils tool = new myUntils();
        /// <summary>
        /// 读取前端配置的json
        /// </summary>
        /// <param name="obj"></param>
        public void readConfig(InTable obj)
        {
            if ( obj==null) return;
            if (!string.IsNullOrWhiteSpace(obj.name )) name = obj.name;
            if (!string.IsNullOrWhiteSpace(obj.key)) key = obj.key;
            if (!string.IsNullOrWhiteSpace(obj.position))
            {
                position = Convert.ToInt32(obj.position);
            };
            if (!string.IsNullOrWhiteSpace(obj.caption)) caption = obj.caption;
            if (!string.IsNullOrWhiteSpace(obj.DBName)) DBName = obj.DBName;
            if (!string.IsNullOrWhiteSpace(obj.selectSQL)) selectSQL = obj.selectSQL;//dataRowNum
            if (!string.IsNullOrWhiteSpace(obj.dataRowNum)) dataRowNum = obj.dataRowNum;
            if (!string.IsNullOrWhiteSpace(obj.batchUpdate)) batchUpdate = obj.batchUpdate == "YES";
            if (!string.IsNullOrWhiteSpace( obj.type))
            {
                mode = (writeMode)Enum.Parse(typeof(writeMode), obj.type);
            }
            else
            {
                mode = root.mode;
            }
            if (!string.IsNullOrWhiteSpace(obj.failPolicy))
            {
                failPolicy = (checkFailAct)Enum.Parse(typeof(checkFailAct), obj.failPolicy);
            }
            else
            {
                failPolicy = checkFailAct.self;
            }
            if (!string.IsNullOrWhiteSpace(obj.useShareCol)) useShareCol = obj.useShareCol == "YES";
            if (!string.IsNullOrWhiteSpace(obj.repeatWhere)) repeatWhere = obj.repeatWhere;
            if (!string.IsNullOrWhiteSpace(obj.updateWhere)) updateWhere = obj.updateWhere;

            //repeatErrTip
            if ( !string.IsNullOrWhiteSpace(obj.repeatErrTip)) repeatErrTip = obj.repeatErrTip;

            if (obj.whereIn != null)
            {
                var whIn = obj.whereIn;
                if (whIn != null && whIn.Count>0)
                {
                    //格式 whereIn:[{field:"",src:"col1,col2"}],
                    foreach (var ob in whIn)
                    {
                        if (ob.field == null) continue;
                        var field = ob.field;
                        var srcs = ob.src;
                        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(srcs))
                            {
                                continue;
                            }
                        if (srcs.Contains(","))
                        {
                            foreach (var src in srcs.Split(','))
                            {
                                this.addWhereInCol(field, src);
                            }
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(repeatWhere) == false)
                {
                    var ExcelCKCols = repeatWhere.Split(';');
                    foreach (var exck in ExcelCKCols)
                    {
                        var exckArr = exck.Split('=');
                        if (exckArr.Length > 1)
                        {
                            this.addWhereInCol(exckArr[0], exckArr[1]);
                        }
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(obj.baseCols))
            {
                var baseCols = obj.baseCols;
                var cp = baseCols.Split(',');
                foreach (var c in cp)
                {
                    tool.ListAdd(selectFields, c);
                }
            }
            if (!string.IsNullOrWhiteSpace(obj.baseWhere)) baseWhere = obj.baseWhere;
            if (!string.IsNullOrWhiteSpace(obj.keyCol)) keyCol = obj.keyCol;


            if (!string.IsNullOrWhiteSpace(obj.repeatBackKeys)) repeatBackKeys = obj.repeatBackKeys; //
            if (!string.IsNullOrWhiteSpace(obj.dynamic)) dynamic = obj.dynamic == "YES";
            if (string.IsNullOrWhiteSpace(DBName)) DBName = name;
            if (string.IsNullOrWhiteSpace(caption)) caption = name;
            if (string.IsNullOrWhiteSpace(keyCol)) keyCol = DBName + "OID";


            //解析重算列
            if (!string.IsNullOrWhiteSpace(obj.reComputeCols))
            {
                var cols = obj.reComputeCols.ToString();
                if (cols.Contains(","))
                {
                    var carr = cols.Split(',');
                    foreach (var c in carr)
                    {
                        if (string.IsNullOrWhiteSpace(c) == false && reComputeCols.Contains(c) == false) reComputeCols.Add(c);
                    }
                }
                else if (string.IsNullOrWhiteSpace(cols) == false) reComputeCols.Add(cols);
            }
            if (obj.KVs != null)
            {
                var fieldsArr = obj.KVs ;
                if (fieldsArr != null && fieldsArr.Count>0)
                {
                    //解析 额外的映射列
                    foreach (var col in fieldsArr)
                    {
                        if (col.field == null) continue;

                        var co = new Column(this.root);
                        co.table = this;
                        co.readConfig(col);
                        co.isField = true;
                        co.tableName = key;
                        if (string.IsNullOrWhiteSpace(co.field) == false)
                        {
                            if (string.IsNullOrWhiteSpace(co.caption))
                            {
                                co.caption = co.field;
                            }
                        }
                        if (co.type == columnType.dynamic)
                        {
                            this.dynamic = true;
                        }
                        KVs.Add(co);
                    }
                }
            }


        }
        private void addWhereInCol(string fieldName, string colKey)
        {
            if (string.IsNullOrWhiteSpace(colKey)) return;
            if (whereInFields.ContainsKey(fieldName) == false)
            {
                var tar = new List<string>() { colKey };
                whereInFields.Add(fieldName, tar);
            }
            else if (whereInFields[fieldName].Contains(colKey) == false)
            {
                whereInFields[fieldName].Add(colKey);
            }

        }
 
        /// <summary>
        /// 根据列字段名获取到列定义对象，不存在返回null
        /// </summary>
        /// <param name="FieldName"></param>
        /// <returns></returns>
        public Column getColumnByFieldName(string FieldName)
        {
            for (int i = 0; i < KVs.Count; i++)
            {
                if (KVs[i].field == FieldName)
                {
                    return KVs[i];
                }
            }
            return null;
        }
        /// <summary>
        /// 根据列的自定义key获取到列定义对象，不存在返回null
        /// </summary>
        /// <param name="KeyName"></param>
        /// <returns></returns>
        public Column getColumnByKeyName(string KeyName)
        {
            for (int i = 0; i < KVs.Count; i++)
            {
                if (KVs[i].key == KeyName)
                {
                    return KVs[i];
                }
            }
            return null;
        }
    }
}
