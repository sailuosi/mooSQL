using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public abstract partial class SQLExpression
    {
        protected string _provideType = "";
        protected string _paraPrefix = "";

        protected string _selectAutoIncrement = "";

        protected string _sentenceSeprator = ";";
        /// <summary>
        /// SQL语句的参数化前缀
        /// </summary>
        /// <returns></returns>
        public string paraPrefix
        {
            get
            {
                return _paraPrefix;
            }
        }
        /// <summary>
        /// 自增关键字
        /// </summary>
        public string selectAutoIncrement
        {
            get
            {
                return _selectAutoIncrement;
            }
        }
        /// <summary>
        /// SQL语句分隔符
        /// </summary>
        public string SentenceSeprator
        {
            get
            {
                return this._sentenceSeprator;
            }
            set
            {
                this._sentenceSeprator = value;
            }
        }
        /// <summary>
        /// 对关键字进行包裹，以规避字段为关键字等的问题
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string wrapKeyword(string value);

        /// <summary>
        /// 对成员进行包裹，以规避字段为关键字等的问题
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string wrapTable(string value)
        {
            return wrapKeyword(value);
        }
        /// <summary>
        /// 包裹字段
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string wrapField(string value)
        {
            if (value == null) {
                return value;
            }
            var val = value.Trim();
            //如果不是由字母和数字组成的，不是纯SQL字段，则直接返回
            var reg=@"^[a-zA-Z0-9_]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(val, reg)) { 
                return value;
            }
            return wrapKeyword(value);
        }
        /// <summary>
        /// 处理表名声明为别名的SQL片段
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="asToName"></param>
        /// <param name="shemaName"></param>
        /// <returns></returns>
        public virtual string wrapTableAsSQL(string tableName,string asToName,string shemaName = "")
        {
            var res = "";
            if (!string.IsNullOrWhiteSpace(shemaName)){ 
                res= string.Concat(res, shemaName, ".");
            }
            res = string.Concat(res, tableName);
            if (!string.IsNullOrWhiteSpace(asToName)) { 
                res = string.Concat(res, " AS ", asToName);
            }
            return res;
        }
    }
}
