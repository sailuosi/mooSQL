// 基础功能说明：

using mooSQL.excel.context;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    public abstract partial class ExcelRead
    {
        #region 逻辑无关--工具方法
        /// <summary>
        /// 为支持用户自定义消息格式而设置
        /// </summary>
        /// <param name="customtip"></param>
        /// <param name="tip"></param>
        /// <param name="logtype"></param>
        /// <returns></returns>
        private string relaceAndPushTip(string customtip, string tip, string logtype)
        {
            var res = Regex.Replace(customtip, "{auto}", tip);
            this.pushLog(res, logtype);
            return res;
        }
        /// <summary>
        /// 某个字符串是否满足正则校验
        /// </summary>
        /// <param name="checkStr"></param>
        /// <param name="Regstr"></param>
        /// <returns></returns>
        public bool isMatch(string checkStr, string Regstr)
        {
            if (string.IsNullOrWhiteSpace(Regstr)) return false;
            Regex reg;
            if (context.option != null && context.option.ignoreCase)
            {
                reg = new Regex(Regstr, RegexOptions.IgnoreCase);
            }
            else
            {
                reg = new Regex(Regstr);
            }
            bool res = false;
            if (checkStr == Regstr || reg.IsMatch(checkStr) || checkStr.IndexOf(Regstr) != -1)
            {
                res = true;
            }
            return res;
        }


        /// <summary>
        /// 是否为有效字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool isValid(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public void addExcelColValue(int colIndex, string value)
        {
            if (value == null || value == "")
            { //作为校验的值，不允许为空。
                return;
            }
            if (!this.excelCheckColData.ContainsKey(colIndex))
            {
                this.excelCheckColData.Add(colIndex, new List<string>());
            }
            if (!this.excelCheckColData[colIndex].Contains(value))
            {
                this.excelCheckColData[colIndex].Add(value);
            }
        }
        /// <summary>
        /// 从数据库获取代码表的数据
        /// </summary>
        /// <param name="codetableId"></param>
        public void addCodeTable(string codetableId)
        {

            //准备代码表的值
            if (!context.valueCollection. codeTableMap.ContainsKey(codetableId))
            {
                //CodeValue codeValue = new CodeValue();
                //DataSet ct = codeValue.GetCodeTable(codetableId);
                var tar = getCodeNameToIdMap(codetableId);
                context.valueCollection.codeTableMap.Add(codetableId, tar);
            }
        }
        public abstract Dictionary<string, string> getCodeNameToIdMap(string codetableId);
        public void addExcelCheckCol(string colname)
        {
            if (!this.excelCheckColnames.Contains(colname))
            {
                this.excelCheckColnames.Add(colname);
            }
        }
        public void addExcelCheckColIndex(int colIndex)
        {
            if (!this.excelCheckColIndex.Contains(colIndex))
            {
                this.excelCheckColIndex.Add(colIndex);
            }
        }
        public void tbInfoAdd(Dictionary<string, WriteTable> map, string tbname, WriteTable tbinfo)
        {
            if (map.ContainsKey(tbname))
            {
                map[tbname] = tbinfo;
            }
            else
            {
                map.Add(tbname, tbinfo);
            }
        }
        public DataTable getBaseDataTable(string tableKey)
        {

            if (baseTable.ContainsKey(tableKey))
            {
                return baseTable[tableKey].table;

            }
            else
            {
                return null;
            }
        }
        public void maplistAdd(Dictionary<string, List<string>> map, string key, string col)
        {
            if (key == "")
            {
                return;
            }
            if (!map.ContainsKey(key))
            {
                var list = new List<string>();
                list.Add(col);
                map.Add(key, list);
            }
            //map已有本列表
            map[key].AddNotRepeat( col);
        }
        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="key"></param>
        public abstract void removeCache(string key);
        /// <summary>
        /// 获取缓存的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string getCacheValue(string key);
        /// <summary>
        /// 设置缓存的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void setCacheValue(string key, string value);
        #endregion
    }
}