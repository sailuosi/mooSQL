// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel.context
{
    /// <summary>
    /// 配置的表
    /// </summary>
    public partial class Table
    {
        //一组助手类方法


        /// <summary>
        /// 添加一个字段，无其他定义信息，请继续定义其他信息，否则无法正常导入！
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public Column addField(string fieldName)
        {
            var col = new Column(this.root);
            col.field = fieldName;
            KVs.Add(col);
            return col;
        }

        /// <summary>
        /// 以一组扁平参数向当前表追加一列配置（字段、来源、校验、写入模式等），并加入 <see cref="Table.KVs"/>。
        /// </summary>
        /// <param name="field">数据库字段名。</param>
        /// <param name="value">固定值或函数表达式（依列类型）。</param>
        /// <param name="key">列在配置中的唯一 key。</param>
        /// <param name="colType">值类型（字符串、数值等）。</param>
        /// <param name="src">来源列或映射说明。</param>
        /// <param name="excelCol">Excel 列标题或列名。</param>
        /// <param name="excelCode">Excel 列编码（若有）。</param>
        /// <param name="codeTable">码表名或关联字典。</param>
        /// <param name="failCode">校验失败时的错误码或提示键。</param>
        /// <param name="mode">写入模式。</param>
        /// <param name="type">列类型（匹配、函数、计算等）。</param>
        /// <param name="rule">校验规则字符串。</param>
        /// <param name="defaultVal">缺省值。</param>
        /// <param name="select">下拉/查询用的 SELECT 片段或字段列表。</param>
        /// <param name="from">查询 FROM 子句或表名。</param>
        /// <param name="where">查询 WHERE 条件。</param>
        /// <param name="reckonType">计算列类型或算法标识。</param>
        /// <param name="seprator">分隔符（拆分/拼接用）。</param>
        /// <param name="range">取值范围或动态列范围描述。</param>
        /// <param name="reg">匹配用正则表达式。</param>
        /// <param name="splitStr">拆分用源字符串或模式。</param>
        /// <param name="splitResName">拆分结果列名前缀或标识。</param>
        /// <param name="splitHeads">拆分后各列对应表头（若有）。</param>
        /// <param name="dynamic">是否为动态列。</param>
        /// <param name="isNeed">是否必填/必需列。</param>
        /// <param name="showTip">是否在界面上展示该列相关提示。</param>
        /// <returns>当前表实例，便于链式调用。</returns>
        public Table add(string field,string value, string key, valueType colType,
            string src,string excelCol,string excelCode,            
            string codeTable, string failCode,writeMode mode,columnType type,string rule, string defaultVal, 
            string select,string from,string where,
            string reckonType,string seprator,
            string range,string reg,string splitStr,string splitResName,string splitHeads,bool dynamic,
            bool isNeed = false,bool showTip=true
            ) {
            var col = new Column(this.root);
            col.field=field;
            col.value=value;
            col.key=key;
            col.colType =colType; 

            col.src=src; 
            col.excelCol=excelCol;   
            col.excelCode=excelCode;

            col.codeTable=codeTable;
            col.failCode=failCode;
            col.mode = mode;
            col.type=type; 
            col.rule=rule;
            col.defaultValue=defaultVal;

            col.select=select;
            col.from=from;
            col.where=where;

            col.reckonType=reckonType;
            col.seprator=seprator;

            col.range=range;
            col.reg=reg;
            col.splitStr=splitStr;
            col.splitResName=splitResName;
            col.splitHeads=splitHeads;

            col.isNeed = isNeed;
            col.dynamic=dynamic;
            col.showTip=showTip;

            KVs.Add(col);

            return this; 
        }

        /// <summary>
        /// 添加固定值列
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Column addFixField(string fieldName, string value)
        {
            var col = new Column(this.root);
            col.field = fieldName;
            col.value = value;
            KVs.Add(col);
            return col;
        }
        /// <summary>
        /// 添加Excel匹配列名
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="excelColName"></param>
        /// <returns></returns>
        public Column addMatchField(string fieldName, string excelColName)
        {
            var col = new Column(this.root);
            col.field = fieldName;
            col.excelCol = excelColName;
            KVs.Add(col);
            return col;
        }
        /// <summary>
        /// 添加全部7个系统字段，不包含删除标记
        /// </summary>
        /// <param name="userOID">用户主键</param>
        /// <param name="postOID">岗位主键</param>
        /// <param name="orgOID">单位主键</param>
        /// <param name="DivisionOID">部门主键</param>
        public void addSysField(object userOID, object postOID, object orgOID, object DivisionOID)
        {
            addFixField("SYS_Created", DateTime.Now.ToString());
            //KVs.Add(createdCol);
            addFixField("SYS_LAST_UPD", DateTime.Now.ToString());
            //KVs.Add(updatedCol);
            addFixField("SYS_CreatedBy", userOID.ToString());
            //KVs.Add(authorCol);
            addFixField("SYS_LAST_UPD_BY", userOID.ToString());
            //KVs.Add(upaurhoCol);
            addFixField("SYS_POSTN", postOID.ToString());
            //KVs.Add(postCol);
            addFixField("SYS_ORG", orgOID.ToString());
            //KVs.Add(orgCol);
            addFixField("SYS_DIVISION", DivisionOID.ToString());
            //KVs.Add(divCol);
        }
        /// <summary>
        /// 数据直接来源于其他字段的字段。
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="srcKey">来源字段的key</param>
        /// <returns></returns>
        public Column addSrcField(string fieldName, string srcKey)
        {
            var col = new Column(this.root);
            col.field = fieldName;
            col.src = srcKey;
            KVs.Add(col);
            return col;
        }

        /// <summary>
        /// 添加固定列范围的动态列，范围由range确定。
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="colRange">范围字符串</param>
        /// <returns></returns>
        public Column addDynamicField(string fieldName, string colRange)
        {
            var col = new Column(this.root);
            col.field = fieldName;
            col.range = colRange;
            col.type = columnType.dynamic;
            KVs.Add(col);
            return col;
        }
        /// <summary>
        /// 添加固定列范围的动态列，范围由正则表达式regStr确定。
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="regStr">正则表达式字符串</param>
        /// <returns>返回列定义对象</returns>
        public Column addDynamicFieldByReg(string fieldName, string regStr)
        {
            var col = new Column(this.root);
            col.field = fieldName;
            col.reg = regStr;
            col.type = columnType.dynamic;
            KVs.Add(col);
            return col;
        }
    }
}
