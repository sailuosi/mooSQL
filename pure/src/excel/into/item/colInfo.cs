
using System;
using System.Collections.Generic;
using System.Data;

using System.Text;
using System.Text.RegularExpressions;




namespace mooSQL.excel.context
{
    /// <summary>
    /// 处理过程中的列信息对象
    /// </summary>
    public class colInfo //:ImportOption.Column
    {
        /// <summary>
        /// 列的原始配置信息，如果不是配置生成的为null
        /// </summary>
        public Column option;
        /// <summary>
        /// 工作环境类指针
        /// </summary>
        public ExcelRead root;
        /// <summary>
        /// 所属的dataTable，只有是字段的列集合存在。
        /// </summary>
        public WriteTable table;
        /// <summary>
        /// 是否是主键列，主键列比较特殊，其值一般只在插入时才获取，更新时是查询获取，其它时候应为空。
        /// </summary>
        public bool isPrimaryKey = false;
        /// <summary>
        /// 写入锁。用于禁用列值赋予，以保障其不被意外修改。
        /// </summary>
        public bool writeLocked = false;

        public string ID = "";
        /// <summary>
        /// 真正写入的列值
        /// </summary>
        public string writeValue;
        /// <summary>
        /// 已被解析类型的对象值
        /// </summary>
        public Object parsedValue;

        /// <summary>
        /// 可插入
        /// </summary>
        public bool canInsert = true;
        /// <summary>
        /// 可修改
        /// </summary>
        public bool canUpdate = true;
        /// <summary>
        /// 已解析的校验规则
        /// </summary>
        public RuleCollection parsedRules;
        /// <summary>
        /// 类型分为2种，已有值done,需要查找 todo
        /// </summary>
        public string state = "todo";
        /// <summary>
        /// 缺省目标列；当该列的取不到，如果设置本属性，尝试去取该列的值
        /// </summary>
        public string defaultCol = "";
        /// <summary>
        /// 值类型
        /// </summary>
        public Type dataType;
        /// <summary>
        /// 是否已设置类型
        /// </summary>
        public bool isSetType = false;
        /// <summary>
        /// 来自excel列的属性
        /// </summary>
        public List<Regex> matchRegs = new List<Regex>();
        /// <summary>
        /// 对应的Excel列名指针 需要在解析表后获取
        /// </summary>
        public int ExcelIndex = -1; 
        /// <summary>
        /// 已被格式化的where条件，可被Format方法直接注入参数
        /// </summary>
        public string formatWhere = "";
        /// <summary>
        /// 初始传入的查询字符串。
        /// </summary>
        public string oldStr = ""; 
        /// <summary>
        /// 字符模式下，查询值未解析时的部分。
        /// </summary>
        public string valuePartStr = "";
        /// <summary>
        /// 源列
        /// </summary>
        public List<string> srcCols = new List<string>();
        //计算列属性
        //private List<string> innerFuncs = new List<string>() { "newid", "getdate" };
        /// <summary>
        /// 切割模式下的切割结果。
        /// </summary>
        public List<string> splitValues = new List<string>();//
                                                             //动态列专用属性 cross
        /// <summary>
        /// 动态的列范围，在表数据循环之前可确定。
        /// </summary>
        public List<string> dynamicExcelCols = new List<string>();
        /// <summary>
        /// 已解析的动态范围
        /// </summary>
        public List<string> parsedRange = new List<string>();
        /// <summary>
        /// 当前焦点列的名字
        /// </summary>
        public string focusColCode;
        /// <summary>
        /// 焦点指针；
        /// </summary>
        public int focusIndex;
                              //public string isDynamic = "NO";//是否焦点变动时需要重新查询值。所有和动态列关联的列，都必须设置为YES.
        /// <summary>
        /// 动态列的跟随标题列，使用该属性指向动态列核心值列。
        /// </summary>
        public string bossID;
        /// <summary>
        /// 奴隶焦点标题列。
        /// </summary>
        public List<string> slaveHeads = new List<string>();//
        /// <summary>
        /// 来源的数据列位置
        /// </summary>
        public int colIndex;
        /// <summary>
        /// 来源的Excel数据行位置
        /// </summary>
        public int rowIndex;
        /// <summary>
        /// 与之关联上的字段。
        /// </summary>
        public List<string> matchedCols = new List<string>();//
                                                             //表内写入时专用属性

        //查询专用属性
        /// <summary>
        /// 是否需要收集列值集合。
        /// </summary>
        public bool needGather = false;
        /// <summary>
        /// 唯一的列值集合。
        /// </summary>
        public List<string> uniValues = new List<string>();
        //旧属性，待清理
        public int num; //字段序号，传入时的字段在字段数组中的编号，从0开始，默认-1
                        //private myUntils tool = new myUntils();
                        //访问属性
        /// <summary>
        /// 配置的key
        /// </summary>
        public string key { get { return option.key; } set { option.key = value; } }
        /// <summary>
        /// 字段
        /// </summary>
        public string field { get { return option.field; } }
        /// <summary>
        /// 表名
        /// </summary>
        public string tableName { get { return option.tableName; } set { option.tableName = value; } }
        /// <summary>
        /// 列类型
        /// </summary>
        public valueType colType { get { return option.colType; } set { option.colType = value; } }
        /// <summary>
        /// 类型
        /// </summary>
        public columnType type { get { return option.type; } set { option.type = value; } }
        /// <summary>
        /// 写入模式
        /// </summary>
        public writeMode mode { get { return option.mode; } set { option.mode = value; } }
        /// <summary>
        /// 配置值
        /// </summary>
        public string value { get { return option.value; } set { option.value = value; } }
        /// <summary>
        /// excel列名
        /// </summary>
        public string excelCol { get { return option.excelCol; } set { option.excelCol = value; } }
        /// <summary>
        /// excel列号
        /// </summary>
        public string excelCode { get { return option.excelCode; } set { option.excelCode = value; } }
        /// <summary>
        /// 标题
        /// </summary>
        public string caption { get { return option.caption; } set { option.caption = value; } }
        /// <summary>
        /// 是否必填
        /// </summary>
        public bool isNeed { get { return option.isNeed; } set { option.isNeed = value; } }
        /// <summary>
        /// 是否提示
        /// </summary>
        public bool showTip { get { return option.showTip; } set { option.showTip = value; } }
        /// <summary>
        /// 代码表
        /// </summary>
        public string codeTable { get { return option.codeTable; } set { option.codeTable = value; } }
        /// <summary>
        /// 缺省值
        /// </summary>
        public string defaultValue { get { return option.defaultValue; } set { option.defaultValue = value; } }
        /// <summary>
        /// 校验规则
        /// </summary>
        public string rule { get { return option.rule; } set { option.rule = value; } }
        /// <summary>
        /// 正则
        /// </summary>
        public string reg { get { return option.reg; } set { option.reg = value; } }
        /// <summary>
        /// 动态
        /// </summary>
        public bool dynamic { get { return option.dynamic; } set { option.dynamic = value; } }
        /// <summary>
        /// 来源列
        /// </summary>
        public string src { get { return option.src; } set { option.src = value; } }
       /// <summary>
       /// 范围
       /// </summary>
        public string range { get { return option.range; } set { option.range = value; } }
        /// <summary>
        /// 分隔符
        /// </summary>
        public string seprator { get { return option.seprator; } set { option.seprator = value; } }
        //查询列的特殊属性
        /// <summary>
        /// 值来源表名
        /// </summary>
        public string selectTable { get { return option.from; } set { option.from = value; } } 
        /// <summary>
        /// 值来源列名
        /// </summary>
        public string selectCol { get { return option.select; } set { option.select = value; } } //
        /// <summary>
        /// 查询条件
        /// </summary>
        public string selectWhere { get { return option.where; } set { option.where = value; } } //
        /// <summary>
        /// 列信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="root"></param>
        public colInfo(string key, ExcelRead root)
        {
            if (this.option == null) this.option = new Column(root.context.option);
            this.key = key;
            this.num = -1;
        }
        /// <summary>
        /// 列信息
        /// </summary>
        public colInfo() { }
        /// <summary>
        /// 列信息
        /// </summary>
        /// <param name="opt"></param>
        public colInfo(Column opt) { this.option = opt; }
        /// <summary>
        /// 依据列的表类型设置列类型
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public bool setColTypeByDB(DataTable dt)
        {
            if (string.IsNullOrWhiteSpace(field) || dt == null || !dt.Columns.Contains(field))
            {
                return false;
            }
            try
            {
                var tarColumn = dt.Columns[field];
                var DBType = tarColumn.DataType;
                dataType = DBType;
                if (colType == valueType.none) { 
                    switch (DBType.Name)
                    {
                        case "String":
                            colType = valueType.stringi;
                            break;
                        case "DateTime":
                            colType = valueType.date;
                            break;
                        case "Guid":
                            colType = valueType.guid;
                            break;
                        case "Boolean":
                            colType = valueType.boolean;
                            break;
                        case "Decimal":
                        case "Int64":
                        case "Int32":

                            colType = valueType.number;
                            break;
                        default:
                            colType = valueType.stringi;
                            break;
                    }                
                }

            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 读取函数
        /// </summary>
        public void readyFunc()
        {
            if (this.type == columnType.function && option.innerFuncs.Contains(value) == false)
            {
                state = "done";
                writeValue = value;
            }
        }
        /// <summary>
        /// 获取函数列的函数值
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void getFuncValue()
        {
            if (this.type == columnType.function)
            {
                if (value == "newid")
                {
                    var oid = Guid.NewGuid();
                    parsedValue = oid;
                    writeValue = oid.ToString();
                }
                else if (value == "getdate")
                {
                    var n = DateTime.Now;
                    writeValue = n.ToString();
                    parsedValue = n;
                }
                else
                {
                    throw new Exception("未能识别的内置function列" + value);
                }
            }
        }
        /// <summary>
        /// 核验列配置
        /// </summary>
        public void chekConfig()
        {
            this.parseRule();
            if (mode == writeMode.insert)
            {
                canUpdate = false;
            }
            else if (mode == writeMode.update)
            {
                canInsert = false;
            }
            else if (mode == writeMode.check)
            {
                canInsert = false;
                canUpdate = false;
            }


            //当未传入列的类型时,自动初始化判断列的类型
            if (type == columnType.none || type == columnType.fix)
            {
                if (string.IsNullOrWhiteSpace(excelCol) == false)
                {//指定了来源的excel列名，即为excel列。
                    type = columnType.match;
                    if (string.IsNullOrWhiteSpace(caption)) caption = excelCol;
                }
                else if (string.IsNullOrWhiteSpace(excelCode) == false)
                {//指定了来源的excel列名，即为excel列。
                    type = columnType.match;
                }
                else if (string.IsNullOrWhiteSpace(option.select) == false)
                {
                    type = columnType.select;
                }
                else if (string.IsNullOrWhiteSpace(option.reckonType) == false)
                {
                    type = columnType.reckon;
                }
                else if (string.IsNullOrWhiteSpace(option.cell) == false)
                {
                    type = columnType.cell;
                }
                else if (option.innerFuncs.Contains(value) == true)
                {
                    type = columnType.function;
                    readyFunc();
                }
                else
                {
                    type = columnType.fix;
                }
            }
            if (string.IsNullOrWhiteSpace(caption))
            {
                if (string.IsNullOrWhiteSpace(option.excelCol) == false)
                {
                    caption = option.excelCol;
                }
                else
                {
                    caption = key;
                }
            }
            if (type == columnType.match)
            {
                if (string.IsNullOrWhiteSpace(excelCol) == false)
                {
                    if (excelCol.Contains("&&"))
                    {
                        var ecs = Regex.Split(excelCol, @"&&");
                        foreach (var ec in ecs)
                        {
                            matchRegs.Add(new Regex(ec));
                        }
                    }
                    else
                    {
                        matchRegs.Add(new Regex(excelCol));
                    }
                }
            }

        }
        /// <summary>
        /// 获取代码值
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public string getValueByCodeMap(string label)
        {
            if (label == null) return "";
            var spReg = new Regex(@"[；;，,.|]");

            if (spReg.IsMatch(label))
            {

                var ress = new List<string>();
                var labels = spReg.Split(label);
                foreach (var la in labels)
                {
                    option.tool.ListAdd(ress, getCodeValue(la));
                }
                var res = new StringBuilder();
                foreach (var la in ress)
                {
                    if (res.Length > 0) res.Append(";");
                    res.Append(la);
                }
                if (res.Length > 0) return res.ToString();
                else return label;
            }
            else
            {
                return getCodeValue(label);
            }
        }
        private string getCodeValue(string label)
        {
            if (option.codeMap.ContainsKey(label))
            {
                return option.codeMap[label];
            }
            else
            {
                foreach (var kv in option.codeMap)
                {
                    if (option.tool.isMatch(label, kv.Key))
                    {
                        return kv.Value;
                    }
                }
            }
            return label;
        }

        private void parseRule()
        {
            if (string.IsNullOrWhiteSpace(rule)) return;
            this.parsedRules = new RuleCollection();
            parsedRules.readConfig(rule);
        }
        /// <summary>
        /// 校验规则
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool checkRule(string value)
        {
            if (this.option.onCheckRule != null)
            {
                return option.onCheckRule(this);
            }
            if (parsedRules == null) return true;
            return parsedRules.check(value, colType);
        }
    }

}
