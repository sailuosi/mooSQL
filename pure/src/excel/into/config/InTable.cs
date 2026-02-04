// 基础功能说明：


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.excel {
    /// <summary>
    /// 导入的表配置
    /// </summary>
    public class InTable
    {
        /// <summary>
        /// 必填，真实数据库表名  PX_Class
        /// </summary>
        public string name = "";

        public string position;

        public string dataRowNum;
        /// <summary>
        /// 索引名，可不设置，为旧版保留属性。
        /// </summary>
        public string key = "";
        /// <summary>
        /// 表中文名，提示用，空缺时使用name名
        /// </summary>
        public string caption = "";
        /// <summary>
        /// 真实的完整数据库表名，空缺时使用name名
        /// </summary>
        public string DBName = "";
        /// <summary>
        /// 自定义表初始化查询时的SQL语句，默认自动根据列信息进行构建。一般不用设置。
        /// </summary>
        public string selectSQL = "";
        /// <summary>
        /// 默认为NO=YES/NO. 本表是否执行批量更新，与全局参数含义相同。
        /// </summary>
        public string batchUpdate = "NO";
        /// <summary>
        /// 默认为全局mode; 可为check/write/insert/update 读写模式
        /// </summary>
        public string type = "";
        /// <summary>
        /// 默认self，可为row/silent/next/before 当表检验失败时的动作。分别指代中断本行excel数据，静默并继续下个表，中断下个表，中断并清理之前的。建议在多表关联导入时设置本属性。
        /// </summary>
        public string failPolicy = "self";
        /// <summary>
        /// 默认为YES=YES/NO. 是否使用共享写入列的设置，默认使用
        /// </summary>
        public string useShareCol = "YES";
        /// <summary>
        /// 主键，默认取表名+OID, PX_ClassOID
        /// </summary>
        public string keyCol = "";
        /// <summary>
        /// 必填，查重使用的where条件串。C_ClassName=idcard=string;
        /// </summary>
        public string repeatWhere = "";//身份证 定位

        public string updateWhere="";
        /// <summary>
        /// 校验的表记录获取的值范围。其值为字段，多个逗号分开。一般不用设置，自动核查。其值用来生成查询表的wherein条件。
        /// </summary>
        public string checkScope = "";
        /// <summary>
        /// 传入的核验范围列。其值为字段，多个逗号分开。一般不用设置，自动核查
        /// </summary>
        public string baseCols = "";
        /// <summary>
        /// 写入列的映射关系。
        /// </summary>
        public List<InField> KVs;
        /// <summary>
        /// 基本的数据范围条件，可结合权限进行设置。PX_Institution_FK='" + instOID + "'
        /// </summary>
        public string baseWhere = "";
        /// <summary>
        /// 旧版转置列，已废弃。
        /// </summary>
        public string unpivot = "";
        /// <summary>
        /// 查重得到唯一行时，需要对列值库进行数据回写的字段。dodo未得到实际应用。
        /// </summary>
        public string repeatBackKeys = "";
        /// <summary>
        /// 默认为NO=YES/NO. 该写入表，是否动态列写入表。当表内包含动态列时，会自动变为YES。
        /// </summary>
        public string dynamic = "NO";
        /// <summary>
        /// 需要重新计算的列，动态列时使用，设定后，动态列的每一行该列重新计算值，一般用主键或查询列、计算列。col1;col2
        /// </summary>
        public string reComputeCols = "";

        public string repeatErrTip = "";

        public List<InField> whereIn;

        /// <summary>
        /// 添加列，推荐使用命名参数形式调用本方法
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <param name="colType"></param>
        /// <param name="src"></param>
        /// <param name="excelCol"></param>
        /// <param name="excelCode"></param>
        /// <param name="codeTable"></param>
        /// <param name="failCode"></param>
        /// <param name="mode"></param>
        /// <param name="type"></param>
        /// <param name="rule"></param>
        /// <param name="defaultVal"></param>
        /// <param name="select"></param>
        /// <param name="from"></param>
        /// <param name="where"></param>
        /// <param name="reckonType"></param>
        /// <param name="seprator"></param>
        /// <param name="range"></param>
        /// <param name="reg"></param>
        /// <param name="splitStr"></param>
        /// <param name="splitResName"></param>
        /// <param name="splitHeads"></param>
        /// <param name="dynamic"></param>
        /// <param name="isNeed"></param>
        /// <param name="showTip"></param>
        /// <returns></returns>
        public InTable add(string field = "", string value = "", string key = "", string colType = "",
            string src = "", string excelCol = "", string excelCode = "",
            string codeTable = "", string failCode = "", string mode = "", string type = "", string rule = "", string defaultVal = "",
            string select = "", string from = "", string where = "",
            string reckonType = "", string seprator = "",
            string range = "", string reg = "", string splitStr = "", string splitResName = "", string splitHeads = "", string dynamic="No",
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
}

