
using mooSQL.utils;

using System;
using System.Collections.Generic;
using System.Data;


namespace mooSQL.excel.context
{
    /// <summary>
    /// 导入配置列信息对象。
    /// </summary>
    public class Column
    {
        
        /// <summary>
        /// 仅供子类使用的构造器
        /// </summary>
        protected Column() { }
        /// <summary>
        /// 列配置
        /// </summary>
        /// <param name="roo"></param>
        public Column(ImportOption roo)
        {
            this.root = roo;
        }
        /// <summary>
        /// 全局配置
        /// </summary>
        public ImportOption root;
        /// <summary>
        /// 所属表配置
        /// </summary>
        public Table table;
        /// <summary>
        /// 是否字段
        /// </summary>
        public bool isField = false;
        /// <summary>
        /// 列集合的键，无关列必须设置，表内列不设置时以 表名_field名 自动生成，
        /// </summary>
        public string key;
        /// <summary>
        /// 设置导入列信息的唯一标识key
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public Column setKey(string keyName)
        {
            key = keyName;
            return this;
        }
        /// <summary>
        /// 列的中文名
        /// </summary>
        public string caption;
        /// <summary>
        /// 设置列的中文名，用于提示用户。
        /// </summary>
        /// <param name="cap"></param>
        /// <returns></returns>
        public Column setCaption(string cap)
        {
            caption = cap;
            return this;
        }
        /// <summary>
        /// 代码表名
        /// </summary>
        public string codeTable;
        /// <summary>
        /// 设置代码表
        /// </summary>
        /// <param name="ct">代码表的codeTableID</param>
        /// <returns></returns>
        public Column setCodeTable(string ct)
        {
            codeTable = ct;
            return this;
        }
        /// <summary>
        /// 列数据获取失败时的提示，可用{auto}指代默认提示，当自定义load方法时无效。
        /// </summary>
        public string loadErrTip = "{auto}";
        /// <summary>
        /// 代码表解析失败的默认代码。
        /// </summary>
        public string failCode;
        /// <summary>
        /// 自定义代码映射,其中key为代码名，value为代码值。
        /// </summary>
        public Dictionary<string, string> codeMap = new Dictionary<string, string>();
        /// <summary>
        /// 设置列的自定义代码映射
        /// </summary>
        /// <param name="map">key为要翻译的label,value为代码值。</param>
        /// <returns></returns>
        public Column setCodeMap(Dictionary<string, string> map)
        {
            codeMap = map;
            return this;
        }
        /// <summary>
        /// 是否必填，默认false
        /// </summary>
        public bool isNeed = false;
        /// <summary>
        /// 设置是否必填
        /// </summary>
        /// <param name="need"></param>
        /// <returns></returns>
        public Column setNeed(bool need)
        {
            this.isNeed = need;
            return this;
        }
        /// <summary>
        /// 加载单元格数据到中间表dataTable的时刻，传入得到的单元格值，返回false时将中断行写入。
        /// </summary>
        public Func<string, bool> onCheckCellValue;

        /// <summary>
        /// 是否需要在读取excel数据时，即对必填列进行核验。当核验失败，该行数据直接跳过。
        /// </summary>
        public bool preMatch = false;
        /// <summary>
        /// 是否超前匹配
        /// </summary>
        /// <param name="isPreMatch"></param>
        /// <returns></returns>
        public Column setPreMatch(bool isPreMatch)
        {
            this.preMatch = isPreMatch;
            return this;
        }
        /// <summary>
        /// 超前核验的正则表达式，一旦设置即执行。
        /// </summary>
        public string preMatchReg;
        /// <summary>
        /// 超前核验的正则表达式，一旦设置即执行。
        /// </summary>
        /// <param name="preMatchRegx"></param>
        /// <returns></returns>
        public Column setPreMatchReg(string preMatchRegx)
        {
            this.preMatchReg = preMatchRegx;
            this.preMatch = true;
            return this;
        }
        /// <summary>
        /// 超前核验的正则表达式，一旦设置即执行。
        /// </summary>
        /// <param name="preMatchRegx">正则</param>
        /// <param name="failMessage">失败的提示消息</param>
        /// <returns></returns>
        public Column setPreMatchReg(string preMatchRegx, string failMessage)
        {
            this.preMatchReg = preMatchRegx;
            this.preMatch = true;
            this.preMatchMsg = failMessage;
            return this;
        }
        /// <summary>
        /// 超前核验未通过时的消息。
        /// </summary>
        public string preMatchMsg = "格式核验未通过";
        /// <summary>
        /// 设置固定值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Column setFixValue(string value)
        {
            this.value = value;
            this.type = columnType.fix;
            return this;
        }
        /// <summary>
        /// 旧版遗留属性，列对应的表。
        /// </summary>
        public string tableName;
        /// <summary>
        /// 字段名。
        /// </summary>
        public string field;
        /// <summary>
        /// 列的读写模式，控制是插入、更新、校验等
        /// </summary>
        public writeMode mode;
        /// <summary>
        /// 默认值，对应前端设置的default属性，因关键字问题改名。
        /// </summary>
        public string defaultValue;
        /// <summary>
        /// 列值自动替换部分正则表达式
        /// </summary>
        public string replaceReg;
        /// <summary>
        /// 列值自动替换的目标值。默认为空。
        /// </summary>
        public string replaceAs = "";
        /// <summary>
        /// 设置列的缺省值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Column setDefaultValue(string value)
        {
            this.defaultValue = value;
            return this;
        }
        /// <summary>
        /// 是否显示该列的提示，默认true
        /// </summary>
        public bool showTip = true;
        /// <summary>
        /// 列的值类型 支持string/number/date/guid. 不设置，自动从数据库读取。
        /// </summary>
        public valueType colType;
        /// <summary>
        /// 列类型
        /// </summary>
        public columnType type;
        /// <summary>
        /// 列的值配置
        /// </summary>
        public string value = string.Empty;
        /// <summary>
        /// 获取列值的函数。当该函数返回false时，将不再执行默认的取值。
        /// </summary>
        public Func<DataRow, colInfo, bool> onLoadData;
        /// <summary>
        /// 列取值完毕后的处理。
        /// </summary>
        public Action<DataRow, colInfo> onAfterLoadData;
        /// <summary>
        /// 列值核验规则，支持function、表达式、固定函数，格式如：not null,>0等。
        /// </summary>
        public string rule;
        /// <summary>
        /// 设置核验规则。支持function、表达式、固定函数，格式如：not null,>0等。
        /// </summary>
        /// <param name="rulestr"></param>
        /// <returns></returns>
        public Column setRule(string rulestr)
        {
            this.rule = rulestr;
            return this;
        }
        /// <summary>
        /// 执行列的数据值校验
        /// </summary>
        public Func<colInfo, bool> onCheckRule;
        /// <summary>
        /// 取自其他列时，其他列的key。
        /// </summary>
        public string src;
        /// <summary>
        /// 取自excel列时，对应的excel标题列的内容。本列不为空，自动为match列
        /// </summary>
        public string excelCol;
        /// <summary>
        /// 取自excel列时，对应的excel列号，为A-Z,AA等。本列不为空，自动为match列
        /// </summary>
        public string excelCode;
        /// <summary>
        /// 根据列名匹配
        /// </summary>
        /// <param name="excelColCaption"></param>
        /// <returns></returns>
        public Column setExcelCol(string excelColCaption)
        {
            this.excelCol = excelColCaption;
            this.type = columnType.match;
            return this;
        }
        /// <summary>
        /// 根据列位置如A匹配
        /// </summary>
        /// <param name="excelColCode"></param>
        /// <returns></returns>
        public Column setExcelCode(string excelColCode)
        {
            this.excelCode = excelColCode;
            this.type = columnType.match;
            return this;
        }
        /*单元格列*/
        /// <summary>
        /// 单元格
        /// </summary>
        public string cell;
        /// <summary>
        /// 设置值来源的单元格，设置后列直接从某个单元格取值。
        /// </summary>
        /// <param name="cellPosition"></param>
        /// <returns></returns>
        public Column setCell(string cellPosition)
        {
            this.cell = cellPosition;
            this.type = columnType.cell;
            return this;
        }
        /*查询列专用*/
        /// <summary>
        /// 查询时的select的字段
        /// </summary>
        public string select;
        /// <summary>
        /// 来源表的名称
        /// </summary>
        public string from;//本列不为空，自动为select列
        /// <summary>
        /// 来源表的条件
        /// </summary>
        public string where;
        /// <summary>
        /// 设置查询列，将自动添加对应的核验表，如有需要可自定义check类型的table，然后自定义其读取条件。
        /// </summary>
        /// <param name="selectPart">来源字段名</param>
        /// <param name="fromPart">来源表名</param>
        /// <param name="wherePart">查询的where条件</param>
        /// <returns></returns>
        public Column setQuery(string selectPart, string fromPart, string wherePart)
        {
            this.select = selectPart;
            from = fromPart;
            where = wherePart;
            this.type = columnType.select;
            return this;
        }
        /*计算列专用
        * 计算的来源列，定义在value中，多个以;隔开。
        */
        /// <summary>
        /// 计算模式，支持string/number/join/split  .//本列不为空，自动为reckon列
        /// </summary>
        public string reckonType;
        /// <summary>
        /// 当计算方式为拼接join或切割split时，指代拼接或切割符号。
        /// </summary>
        public string seprator;

        /*动态列专用*/
        //标题切割的功能尚未实装todo.
        /// <summary>
        /// 列编号范围，格式类似 A-H,V,I-J   //本列不为空，自动为dynamici列
        /// </summary>
        public string range;
        /// <summary>
        /// 列范围的标题正则表达式。 //本列不为空，自动为dynamici列
        /// </summary>
        public string reg;
        /// <summary>
        /// 当列标题需要切割时，传入切割的字符。 默认为空，即不执行切割。
        /// </summary>
        public string splitStr;
        /// <summary>
        /// 切割的结果列名。需要切割的标题行号，以逗号分隔。
        /// </summary>
        public string splitResName;
        /// <summary>
        /// 切割后形成的列名格式。
        /// </summary>
        public string splitHeads;
        /// <summary>
        /// 是否焦点变动时需要重新查询值。所有和动态列关联的列，都必须设置为YES.
        /// </summary>
        public bool dynamic = false;
        /// <summary>
        /// 设置跟随动态列的变化取值的子字段，如果是标题，以动态列的key+Head+行号。或Index  Code等焦点值
        /// </summary>
        /// <param name="srcKey">来源的列的键</param>
        /// <returns></returns>
        public Column setSlave(string srcKey)
        {
            this.src = srcKey;
            this.dynamic = true;
            return this;
        }
        /// <summary>
        /// 内置函数
        /// </summary>
        public List<string> innerFuncs = new List<string>() { "newid", "getdate" };
        /// <summary>
        /// 工具
        /// </summary>
        public myUntils tool = new myUntils();
        /// <summary>
        /// 读取列信息的配置json
        /// </summary>
        /// <param name="obj"></param>
        public void readConfig(InField obj)
        {

            if (!string.IsNullOrWhiteSpace(obj.key )) key = obj.key;
            //src
            if (!string.IsNullOrWhiteSpace(obj.src)) src = obj.src;
            if (!string.IsNullOrWhiteSpace(obj.field)) field = obj.field;//表内字段名  
            if (!string.IsNullOrWhiteSpace(obj.excelCol)) excelCol = obj.excelCol;
            if (!string.IsNullOrWhiteSpace(obj.excelCode)) excelCode = obj.excelCode;
            if (!string.IsNullOrWhiteSpace(obj.codeTable)) codeTable = obj.codeTable;
            if (obj.codeMap != null)
            {   //值为对象数组，{label:"疗养",value:"1"}
                var cm = obj.codeMap;
                if (cm != null)
                {
                    foreach (var li in cm)
                    {
                        if (li.label == null) continue;
                        tool.mapAdd(codeMap, li.label, li.value);
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(obj.isNeed)) isNeed = obj.isNeed == "YES";
            if (!string.IsNullOrWhiteSpace(obj.tableName)) tableName = obj.tableName;
            if (!string.IsNullOrWhiteSpace(obj.mode))
            {
                var m = obj.mode;
                if (m == "insert")
                {
                    mode = writeMode.insert;
                }
                else if (m == "update")
                {
                    mode = writeMode.update;
                }
                else if (m == "check")
                {
                    mode = writeMode.check;
                }
                else if (m == "write")
                {
                    mode = writeMode.write;
                }
            }
            //loadErrTip
            if (!string.IsNullOrWhiteSpace(obj.loadErrTip)) loadErrTip = obj.loadErrTip;
            if (!string.IsNullOrWhiteSpace(obj.@default)) defaultValue = obj.@default;
            if (!string.IsNullOrWhiteSpace(obj.caption)) caption = obj.caption;
            if (!string.IsNullOrWhiteSpace(obj.failCode)) failCode = obj.failCode;
            if (!string.IsNullOrWhiteSpace(obj.showTip)) showTip = obj.showTip == "YES";
            if (!string.IsNullOrWhiteSpace(obj.colType))
            {
                var jscoltype = obj.colType;
                switch (jscoltype)
                {
                    case "string": colType = valueType.stringi; break;
                    case "date": colType = valueType.date; break;
                    case "number": colType = valueType.number; break;
                    case "guid": colType = valueType.guid; break;
                    case "bool": colType = valueType.boolean; break;
                }
            }
            if (!string.IsNullOrWhiteSpace(obj.caption)) caption = obj.caption;
            if (!string.IsNullOrWhiteSpace(obj.type))
            {
                var jsype = obj.type;
                switch (jsype)
                {
                    case "match": type = columnType.match; break;
                    case "function": type = columnType.function; break;
                    case "fixed": type = columnType.fix; break;
                    case "cell": type = columnType.cell; break;
                    case "reckon": type = columnType.reckon; break;
                    case "select": type = columnType.select; break;
                    case "dynamic": type = columnType.dynamic; break;

                }
            }
            //超前核验部分
            if (!string.IsNullOrWhiteSpace(obj.preMatchReg))
            {
                this.preMatchReg = obj.preMatchReg;
            }
            if (!string.IsNullOrWhiteSpace(obj.preMatchMsg))
            {
                this.preMatchMsg = obj.preMatchMsg;
            }
            //查询列
            if (!string.IsNullOrWhiteSpace(obj.select)) select = obj.select;
            if (!string.IsNullOrWhiteSpace(obj.from)) from = obj.from;
            if (!string.IsNullOrWhiteSpace(obj.where)) where = obj.where;
            if (!string.IsNullOrWhiteSpace(obj.value)) value = obj.value;
            if (!string.IsNullOrWhiteSpace(obj.rule)) rule = obj.rule;
            //计算列专用
            if (!string.IsNullOrWhiteSpace(obj.reckonType)) reckonType = obj.reckonType;
            if (!string.IsNullOrWhiteSpace(obj.seprator)) seprator = obj.seprator;
            //单元格列
            if (!string.IsNullOrWhiteSpace(obj.cell)) cell = obj.cell;
            //动态列格式下的专用属性
            if (!string.IsNullOrWhiteSpace(obj.range))
            {
                range = obj.range;
                type = columnType.dynamic;
            }
            if (!string.IsNullOrWhiteSpace( obj.reg))
            {
                reg = obj.reg;
                type = columnType.dynamic;
            }
            if (!string.IsNullOrWhiteSpace(obj.splitStr)) splitStr = obj.splitStr;
            if (!string.IsNullOrWhiteSpace(obj.splitResName)) splitResName = obj.splitResName;
            if (!string.IsNullOrWhiteSpace(obj.splitHeads)) splitHeads = obj.splitHeads;
            if (!string.IsNullOrWhiteSpace(obj.dynamic)) dynamic = obj.dynamic == "YES";

            if (string.IsNullOrWhiteSpace(caption))
            {
                if (string.IsNullOrWhiteSpace(excelCol) == false)
                {
                    caption = excelCol;
                }
                else { caption = key; }
            }
            this.replaceReg = obj.replaceReg;
            this.replaceAs = obj.replaceAs;
        }
        /// <summary>
        /// 检查自身的参数，进行自动校正
        /// </summary>
        public void checkConfig()
        {

            
            //src


            //动态列格式下的专用属性
            if (!string.IsNullOrWhiteSpace(range))
            {
                type = columnType.dynamic;
            }
            if (!string.IsNullOrWhiteSpace(reg))
            {
                type = columnType.dynamic;
            }


            if (string.IsNullOrWhiteSpace(caption))
            {
                if (string.IsNullOrWhiteSpace(excelCol) == false)
                {
                    caption = excelCol;
                }
                else { caption = key; }
            }
        }
    }
}
