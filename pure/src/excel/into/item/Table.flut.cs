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
