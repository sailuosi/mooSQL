using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// DataTable的扩展
    /// </summary>
    public static class MooTableExtensions
    {

        /// <summary>
        /// 将某个dataTable中的某一列的值存入一个list。且消除重复值。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static List<string> getFieldValues(this DataTable dt, string fieldName)
        {
            var res = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                var ro = row[fieldName].ToString();
                if (res.Contains(ro) == false) res.Add(ro);
            }
            return res;
        }
        /// <summary>
        /// 获取表某个字段的字符串集合
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static List<string> getFieldValues(this DataRow[] rows, string fieldName)
        {
            var res = new List<string>();
            foreach (DataRow row in rows)
            {
                var ro = row[fieldName].ToString();
                if (res.Contains(ro) == false) res.Add(ro);
            }
            return res;
        }
        /// <summary>
        /// 获取表某个字段的字符串集合
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="getFieldValue"></param>
        /// <returns></returns>
        public static List<string> getFieldValues(this DataRow[] rows, Func<DataRow, string> getFieldValue)
        {
            var res = new List<string>();
            foreach (DataRow row in rows)
            {
                var ro = getFieldValue(row);
                if (string.IsNullOrWhiteSpace(ro)) continue;
                if (res.Contains(ro) == false) res.Add(ro);
            }
            return res;
        }
        /// <summary>
        /// 按字段分组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Dictionary<string, List<T>> groupBy<T>(this DataTable dt, string fieldName, Func<DataRow, T> func)
        {
            var res = new Dictionary<string, List<T>>();
            foreach (DataRow row in dt.Rows)
            {
                var v = row[fieldName];
                string val = "";
                if (v == null || v == DBNull.Value)
                {
                    val = "";
                }
                else
                {
                    val = v.ToString();
                }
                if (!res.ContainsKey(val))
                {
                    res.Add(val, new List<T>());
                }
                res[val].Add(func(row));
            }
            return res;
        }
        /// <summary>
        /// 按字段分组
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static Dictionary<string, List<DataRow>> groupBy(this DataTable dt, string fieldName)
        {
            var res = new Dictionary<string, List<DataRow>>();
            foreach (DataRow row in dt.Rows)
            {
                var v = row[fieldName];
                string val = "";
                if (v == null || v == DBNull.Value)
                {
                    val = "";
                }
                else
                {
                    val = v.ToString();
                }
                if (!res.ContainsKey(val))
                {
                    res.Add(val, new List<DataRow>());
                }
                res[val].Add(row);
            }
            return res;
        }
    }
}
