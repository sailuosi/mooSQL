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
        /// <summary>
        /// 向指定 Excel 列索引收集用于 whereIn 等校验的去重单元格值。
        /// </summary>
        /// <param name="colIndex">Excel 列索引。</param>
        /// <param name="value">单元格值（空则忽略）。</param>
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
        /// <summary>
        /// 获取代码表「显示名 → 主键/ID」映射，供导入时转换字典值。
        /// </summary>
        /// <param name="codetableId">代码表标识。</param>
        /// <returns>名称到 Id 的字典。</returns>
        public abstract Dictionary<string, string> getCodeNameToIdMap(string codetableId);
        /// <summary>
        /// 将列名加入需做 Excel 侧 whereIn 校验的列名列表。
        /// </summary>
        /// <param name="colname">列逻辑名或 key。</param>
        public void addExcelCheckCol(string colname)
        {
            if (!this.excelCheckColnames.Contains(colname))
            {
                this.excelCheckColnames.Add(colname);
            }
        }
        /// <summary>
        /// 将 Excel 列索引加入需做 whereIn 校验的列索引列表。
        /// </summary>
        /// <param name="colIndex">Excel 从 0 或约定起的列索引。</param>
        public void addExcelCheckColIndex(int colIndex)
        {
            if (!this.excelCheckColIndex.Contains(colIndex))
            {
                this.excelCheckColIndex.Add(colIndex);
            }
        }
        /// <summary>
        /// 向写入表字典添加或覆盖表名对应的 <see cref="WriteTable"/>。
        /// </summary>
        /// <param name="map">表名字典。</param>
        /// <param name="tbname">表名或逻辑 key。</param>
        /// <param name="tbinfo">写入表实例。</param>
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
        /// <summary>
        /// 获取用于本地校验的基础数据表（存在于 <c>baseTable</c> 时）。
        /// </summary>
        /// <param name="tableKey">核验表 key。</param>
        /// <returns>数据表；不存在则 null。</returns>
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
        /// <summary>
        /// 向「键 → 列名列表」映射追加列名（去重由扩展方法处理）。
        /// </summary>
        /// <param name="map">分组映射。</param>
        /// <param name="key">分组键。</param>
        /// <param name="col">列名或字段名。</param>
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