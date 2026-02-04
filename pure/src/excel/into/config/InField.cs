using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// 导入的表的字段配置
    /// </summary>
    public class InField
    {
        /// <summary>
        /// 列集合的键，无关列必须设置，表内列不设置时以 表名_field名 自动生成，kmOID
        /// </summary>
        public string key="";
        /// <summary>
        /// 列的中文名
        /// </summary>
        public string caption ="";
        /// <summary>
        /// 代码表名
        /// </summary>
        public string codeTable ="";
        /// <summary>
        /// 代码表解析失败的默认代码。
        /// </summary>
        public string failCode ="";
        /// <summary>
        /// 自定义代码映射
        /// </summary>
        public List<CodeItem> codeMap;
            /// <summary>
            /// 是否必填，默认为NO=YES/NO
            /// </summary>
       public string isNeed ="NO";
        /// <summary>
        /// 旧版遗留属性，列对应的表。
        /// </summary>
        public string tableName ="";
        /// <summary>
        /// 列的读写配置，可为 insert/update/check。默认全读写。
        /// </summary>
        public string mode ="";
        /// <summary>
        /// 缺省值;不设置时无缺省值。
        /// </summary>
        public string        @default="";
        /// <summary>
        /// 是否显示该列的提示，默认YES; YES/NO.
        /// </summary>
        public string showTip ="YES";
        /// <summary>
        /// 列的值类型 支持string/number/date/guid. 不设置，自动从数据库读取。
        /// </summary>
        public string colType ="";
        /// <summary>
        /// 列的类型，一般自动判断。包含match/function/reckon/select/fixed/cell ;分别代表/内置方法、计算列、查询语句
        /// </summary>
        public string type ="";
        /// <summary>
        /// 列值
        /// </summary>
        public string value ="";
        /// <summary>
        /// 列值核验规则，支持function、表达式、固定函数，格式如：not null;>0等。
        /// </summary>
        public string rule ="";
        /*excel关联列的专用属性*/

        /// <summary>
        /// 字段名
        /// </summary>
        public string field ="";
        /// <summary>
        /// 取自其他列时，其他列的key。
        /// </summary>
        public string src ="";
        /// <summary>
        /// 取自excel列时，对应的excel标题列的内容。本列不为空，自动为match列
        /// </summary>
        public string excelCol ="";
        /// <summary>
        /// //取自excel列时，对应的excel列号，为A-Z;AA等。本列不为空，自动为match列
        /// </summary>
        public string excelCode ="";

        /*查询列专用*/
        /// <summary>
        /// PX_TrainSubjectOID
        /// </summary>
        public string select ="";
        /// <summary>
        /// 本列不为空，自动为select列 PX_TrainSubject
        /// </summary>
        public string from ="";
        /// <summary>
        /// TS_KMName=kmname=string
        /// </summary>
        public string where ="";

        /*计算列专用
        * 计算的来源列，定义在value中，多个以;隔开。
        */
        /// <summary>
        /// 计算模式，支持string/number/join/split  .//本列不为空，自动为reckon列
        /// </summary>
        public string reckonType ="";
        /// <summary>
        /// 当计算方式为拼接join或切割split时，指代拼接或切割符号。
        /// </summary>
        public string seprator =";";

        /*动态列专用*/
        //标题切割的功能尚未实装todo.

        /// <summary>
        /// 列编号范围，格式类似 A-H;V;I-J   //本列不为空，自动为dynamici列
        /// </summary>
        public string range ="";
        /// <summary>
        /// 列范围的标题正则表达式。 //本列不为空，自动为dynamici列
        /// </summary>
        public string reg ="";
        /// <summary>
        /// 当列标题需要切割时，传入切割的字符。 默认为空，即不执行切割。
        /// </summary>
        public string splitStr ="";
        /// <summary>
        /// 切割的结果列名。需要切割的标题行号，以逗号分隔。
        /// </summary>
        public string splitResName ="";
        /// <summary>
        /// 切割后形成的列名格式。
        /// </summary>
        public string splitHeads ="";
        /// <summary>
        /// 是否焦点变动时需要重新查询值。所有和动态列关联的列，都必须设置为YES.
        /// </summary>
        public string dynamic ="NO";

        public string loadErrTip;

        public string preMatchReg;

        public string preMatchMsg;

        public string cell;

        public string replaceReg;

        public string replaceAs;
    }


    public class CodeItem {
        public string label;
        public string value;
    }
}
