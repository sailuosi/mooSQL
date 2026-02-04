// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel {
    /// <summary>
    /// 导入的全局配置
    ///  标题列
    ///所有的标题以 列号+行号形成列集合，如，A1,B1,C1,等代指第1行的ABC三列的标题。
    /// 动态列
    ///    核心动态列，是类型type为dynamic的列，当列定义了range/reg属性时，自动为动态核心列。
    ///    动态列在解析完毕后，将生成以动态列的key+Head+标题行号为key的所有动态范围列。
    ///    动态列的标题，将以动态列的key+Index,动态列的key+Code，形成2个跟随动态列焦点而变化的列。其列类型固定内化为focusHead。
    ///    例如，在动态列的从列里，如果要访问此时的excel列号，对应列集合的key为 动态核心列的key+Head+该标题的行号，如scoreHead1。
    ///    访问动态列中指针 scoreIndex
    ///    访问动态列中列号 scoreCode
    /// </summary>
    public class InConfig
    {
        /// <summary>
        /// 数据查询模式，默认为local:local/database。一般不需要修改。 分为local内存查询和database数据库查询。内存查询优点是速度快，缺点是准备时间长，数据量越大越明显。数据库查询优点是无前摇，但执行写入速度慢。
        /// </summary>
        public string checkMode = "local";
        /// <summary>
        /// 输出日志时输出[提示]类的信息，默认NO:YES/NO。因提示性的涉及的太多，提示信息过多会导致用户难以找到关键性的信息。
        /// </summary>
        public string logtips = "NO";
        /// <summary>
        /// 默认NO:YES/NO,是否启用批量更新,启用时，将使用StrSQLMaker类的UpdateTable进行批量提交，否则，使用dealUpdate方法生成的SQL逐行更新
        /// </summary>
        public string batchUpdate = "NO";
        /// <summary>
        /// 标题，导入日志的数据库表中，将以它作为标题列的值。
        /// </summary>
        public string title = "导入培训班";
        /// <summary>
        /// 必填，其值为列集合中的某一列的key。行日志下的列标识，输出列信息
        /// </summary>
        public string outInfoCol = "idcard";
        /// <summary>
        /// 默认insert:write/insert/update。分别指代执行同时更新和插入，只插入，只更新。
        /// </summary>
        public string mode = "insert";
        /// <summary>
        /// 默认为 1 ，格式为数字。导入表格的标题所在的excel行号，其值与excel中所显示的行号相同（从1开始）。多个以逗号分隔，如果"1,2,3,5"。
        /// </summary>
        public string titleRowNum = "1";
        /// <summary>
        /// 默认为 2-，即从第2行开始，支持类似于 3,4,9-15,56-这样的格式，即区间以“-”分隔，其值与excel中所显示的行号
        /// </summary>
        public string dataRowNum = "2-";
        /// <summary>
        /// 读取的列范围
        /// </summary>
        public string dataColNum = "";
        /// <summary>
        /// excel导入模板的地址，仅为vue版调起页面专用。aspx版调起页面无效。../../PXGL/ExcelModels/班级管理培训班导入模板.xlsx
        /// </summary>
        public string demoUrl = "";
        /// <summary>
        /// 导入的帮助与提示信息，格式为html文本。仅为vue版调起页面专用。aspx版调起页面无效。本导入模板为固定格式，请不要修改列头、数据体的行位置，本模板用来导入培训中心->培训办班->班级管理  的计划外培训班，计划内的培训班请在创建办班计划后进行创建。<br/>第一列分类，必填，是指系统中左侧的分类中分类名，请确保培训类别名称与分类中的某个分类一致。培训时间，即培训开始时间，是系统判断培训班所在年月的依据，请务必填写！<br/>年度、期次，填写数字即可。也可以按照标准的“2020年”,“第1期”这样的格式写，任选。<br/>主办单位自动设置当前用户的机构，不需要输入，因此，禁止导入非自己管理的培训班。
        /// </summary>
        public string note = "";
        /// <summary>
        /// 传入参数为1个，1参 ExcelRead  
        /// </summary>
        public InBPOMethod beforeSave ;
        /// <summary>
        /// 
        /// </summary>
        public InBPOMethod afterSave ;
        /// <summary>
        /// BPO服务端的导入配置
        /// </summary>
        public InBPOMethod loadConfig;
        /// <summary>
        /// 必填，复合一级属性。为导入所需的目标数据库表，包含导入表、查询表。
        /// </summary>
        public List<InTable> tables;
        /// <summary>
        /// 与表无关的写入列集合。其语法与表内的相同。只是没有field属性。而表内列集合，必须设置field属性。
        /// </summary>
        public List<InField> KVs;

        public List<InField> shareCol;
        /// <summary>
        /// 是否回写excel
        /// </summary>
        public string saveMsgToExcel;
        /// <summary>
        /// 匹配时是否忽视大小写
        /// </summary>
        public string ignoreCase;
        /// <summary>
        /// 
        /// </summary>
        public string logColNum;
        /// <summary>
        /// 
        /// </summary>
        public string titleScanScope;
        /// <summary>
        /// 
        /// </summary>
        public List<string> titleScanReg;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="keyCol"></param>
        /// <param name="repeatWhere"></param>
        /// <param name="baseWhere"></param>
        /// <param name="position"></param>
        /// <param name="dataRowNum"></param>
        /// <param name="key"></param>
        /// <param name="caption"></param>
        /// <param name="DBName"></param>
        /// <param name="selectSQL"></param>
        /// <param name="batchUpdate"></param>
        /// <param name="type"></param>
        /// <param name="failPolicy"></param>
        /// <param name="useShareCol"></param>
        /// <param name="updateWhere"></param>
        /// <param name="checkScope"></param>
        /// <param name="baseCols"></param>
        /// <param name="unpivot"></param>
        /// <param name="repeatBackKeys"></param>
        /// <param name="dynamic"></param>
        /// <param name="reComputeCols"></param>
        /// <param name="repeatErrTip"></param>
        /// <returns></returns>
        public InTable addTable(string name, string keyCol, string repeatWhere, string baseWhere="",
                string position="",
                string dataRowNum="",

                // 索引名，可不设置，为旧版保留属性。
                string key = "",

                // 表中文名，提示用，空缺时使用name名
                string caption = "",

                // 真实的完整数据库表名，空缺时使用name名
                string DBName = "",

                // 自定义表初始化查询时的SQL语句，默认自动根据列信息进行构建。一般不用设置。
                string selectSQL = "",

                // 默认为NO=YES/NO. 本表是否执行批量更新，与全局参数含义相同。
                string batchUpdate = "NO",

                // 默认为全局mode; 可为check/write/insert/update 读写模式
                string type = "",

                // 默认self，可为row/silent/next/before 当表检验失败时的动作。分别指代中断本行excel数据，静默并继续下个表，中断下个表，中断并清理之前的。建议在多表关联导入时设置本属性。
                string failPolicy = "self",

                // 默认为YES=YES/NO. 是否使用共享写入列的设置，默认使用
                string useShareCol = "YES",


                string updateWhere = "",
                // 校验的表记录获取的值范围。其值为字段，多个逗号分开。一般不用设置，自动核查。其值用来生成查询表的wherein条件。

                string checkScope = "",
                // 传入的核验范围列。其值为字段，多个逗号分开。一般不用设置，自动核查

                string baseCols = "",
                // 旧版转置列，已废弃。

                string unpivot = "",

                // 查重得到唯一行时，需要对列值库进行数据回写的字段。dodo未得到实际应用。

                string repeatBackKeys = "",
                // 默认为NO=YES/NO. 该写入表，是否动态列写入表。当表内包含动态列时，会自动变为YES。
                string dynamic = "NO",

                // 需要重新计算的列，动态列时使用，设定后，动态列的每一行该列重新计算值，一般用主键或查询列、计算列。col1;col2
                string reComputeCols = "",

                string repeatErrTip = ""
            )
        {
            var tb = new InTable();
            tb.name = name;
            tb.caption = caption;
            tb.keyCol = keyCol;
            tb.repeatWhere = repeatWhere;
            tb.baseWhere = baseWhere;

            tb.position = position;
            tb.dataRowNum = dataRowNum;
            tb.key = key;
            tb.DBName = DBName;
            tb.selectSQL = selectSQL;
            tb.batchUpdate = batchUpdate;
            tb.type = type;
            tb.failPolicy = failPolicy;
            tb.useShareCol = useShareCol;
            tb.updateWhere = updateWhere;
            tb.checkScope = checkScope;
            tb.baseCols = baseCols;
            tb.unpivot = unpivot;
            tb.repeatBackKeys = repeatBackKeys;
            tb.dynamic = dynamic;
            tb.reComputeCols = reComputeCols;
            tb.repeatErrTip = repeatErrTip;

            tables.Add(tb);
            return tb;
        }

        public InConfig addKV(string field = "", string value = "", string key = "", string colType = "",
    string src = "", string excelCol = "", string excelCode = "",
    string codeTable = "", string failCode = "", string mode = "", string type = "", string rule = "", string defaultVal = "",
    string select = "", string from = "", string where = "",
    string reckonType = "", string seprator = "",
    string range = "", string reg = "", string splitStr = "", string splitResName = "", string splitHeads = "", string dynamic = "No",
    string isNeed = "No", string showTip = "YES"
    )
        {
            var col = new InField();
            col.field = field;
            col.value = value;
            col.key = key;
            col.colType = colType;

            col.src = src;
            col.excelCol = excelCol;
            col.excelCode = excelCode;

            col.codeTable = codeTable;
            col.failCode = failCode;
            col.mode = mode;
            col.type = type;
            col.rule = rule;
            col.@default = defaultVal;

            col.select = select;
            col.from = from;
            col.where = where;

            col.reckonType = reckonType;
            col.seprator = seprator;

            col.range = range;
            col.reg = reg;
            col.splitStr = splitStr;
            col.splitResName = splitResName;
            col.splitHeads = splitHeads;

            col.isNeed = isNeed;
            col.dynamic = dynamic;
            col.showTip = showTip;

            KVs.Add(col);

            return this;
        }
    }
    /// <summary>
    /// BPO调用的配置
    /// </summary>
    public class InBPOMethod {
        public string BPOName;
        public string method;
    }
}
