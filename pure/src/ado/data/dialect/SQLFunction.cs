using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 数据库函数的方言。
    /// </summary>
    public abstract class SooSQLFunction
    {

        public virtual string Concat(string left, string right) {
            throw new NotImplementedException("当前数据库不支持该函数");
        }

        /// <summary>
        /// 计算字符数
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        public virtual string Len(string FieldSQL) {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
        /// <summary>
        /// 截取字符串
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string SubStr(string FieldSQL, int start,int length)
        {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
        /// <summary>
        /// 替换字符串
        /// </summary>
        /// <param name="Str"></param>
        /// <param name="oldStr"></param>
        /// <param name="newStr"></param>
        /// <returns></returns>
        public virtual string Replace(string Str, string oldStr, string newStr) { 
            return string.Concat(" REPLACE( ",Str, oldStr, newStr,")");
        }
        /// <summary>
        /// 查找字符串位置
        /// </summary>
        /// <param name="subString"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string CharIndex(string subString,string str)
        {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
        /// <summary>
        /// 向上取整
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        public virtual string Ceil(string FieldSQL)
        {
            return string.Concat(" CEIL( ", FieldSQL, ")");
        }
        /// <summary>
        /// 向下取整
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        public virtual string Floor(string FieldSQL)
        {
            return string.Concat(" FLOOR( ", FieldSQL, ")");
        }
        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public virtual string Round(string FieldSQL, int decimals)
        {
            return string.Concat(" ROUND( ", FieldSQL,",", decimals, ")");
        }
        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string Now()
        {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
        /// <summary>
        /// 获取年份
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string Year(string FieldSQL)
        {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
        /// <summary>
        /// 获取月份
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string Month(string FieldSQL)
        {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
        /// <summary>
        /// 获取日期
        /// </summary>
        /// <param name="FieldSQL"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string Day(string FieldSQL)
        {
            throw new NotImplementedException("当前数据库不支持该函数");
        }
    }
}
