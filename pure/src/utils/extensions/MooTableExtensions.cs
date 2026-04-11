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
        /// 遍历表行，用 <paramref name="loader"/> 提取值并去重后加入列表（跳过 null）。
        /// </summary>
        /// <typeparam name="T">提取值类型。</typeparam>
        /// <param name="dt">数据表。</param>
        /// <param name="loader">从行映射到值的委托。</param>
        /// <returns>去重后的值列表。</returns>
        public static List<T> getFieldValues<T>(this DataTable dt, Func<DataRow, T> loader)
        {
            List<T> list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                var item = loader(row);
                if (item != null && !list.Contains(item))
                {
                    list.Add(item);
                }
            }

            return list;
        }
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
        /// <summary>
        /// 完全自定义的字典生成方法
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dt"></param>
        /// <param name="loadKey"></param>
        /// <param name="loadV"></param>
        /// <returns></returns>
        public static Dictionary<K,V> groupBy<K,V>(this DataTable dt, Func<DataRow,K> loadKey,Func<DataRow,V> loadV)
        {
            var res = new Dictionary<K, V>();
            foreach (DataRow row in dt.Rows)
            {
                var k = loadKey(row);

                if (k == null )
                {
                    continue;
                }
                var v = loadV(row);
                if (!res.ContainsKey(k))
                {
                    res.Add(k, v);
                    continue;
                }
                res[k]=v;
            }
            return res;
        }
        /// <summary>
        /// 按键分组，每组值为多值列表（同键下值通过 <c>AddNotRepeat</c> 去重追加）。
        /// </summary>
        /// <typeparam name="K">键类型。</typeparam>
        /// <typeparam name="V">值类型。</typeparam>
        /// <param name="dt">数据表。</param>
        /// <param name="loadKey">键选择器。</param>
        /// <param name="loadV">值选择器。</param>
        /// <returns>键到值列表的映射。</returns>
        public static Dictionary<K,List<V>> groupByAsList<K, V>(this DataTable dt, Func<DataRow, K> loadKey, Func<DataRow, V> loadV)
        {
            var res = new Dictionary<K, List<V>>();
            foreach (DataRow row in dt.Rows)
            {
                var k = loadKey(row);

                if (k == null)
                {
                    continue;
                }
                var v = loadV(row);
                if (!res.ContainsKey(k))
                {
                    res.Add(k, new List<V>() {v});
                    continue;
                }
                res[k].AddNotRepeat(v);
            }
            return res;
        }
        /// <summary>
        /// 按2个字段分组
        /// </summary>
        /// <typeparam name="K1"></typeparam>
        /// <typeparam name="K2"></typeparam>
        /// <typeparam name="K3"></typeparam>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldName2"></param>
        /// <param name="loadVal"></param>
        /// <returns></returns>
        public static Dictionary<K1, Dictionary<K2,List<K3>>> groupBy<K1, K2, K3>(this DataTable dt, Func<DataRow, K1> fieldName, Func<DataRow, K2> fieldName2, Func<DataRow, K3> loadVal)
        {
            Dictionary<K1, Dictionary<K2, List<K3>>> dictionary = new Dictionary<K1, Dictionary<K2, List<K3>>>();
            foreach (DataRow row in dt.Rows)
            {
                var k1 = fieldName(row);
                if (k1 == null)
                {
                    continue;
                }
                var k2 = fieldName2(row);
                if (k2 == null)
                {
                    continue;
                }
                var val = loadVal(row);

                if (!dictionary.ContainsKey(k1))
                {
                    dictionary.Add(k1, new Dictionary<K2, List<K3>>());
                }
                if (val == null || dictionary[k1][k2].Contains(val)) {
                    continue;
                }
                dictionary[k1][k2].Add(val);
            }

            return dictionary;
        }
        /// <summary>
        /// 按两级键生成嵌套字典，叶节点为单值（后写覆盖先写）。
        /// </summary>
        /// <typeparam name="K1">外层键类型。</typeparam>
        /// <typeparam name="K2">内层键类型。</typeparam>
        /// <typeparam name="K3">值类型。</typeparam>
        /// <param name="dt">数据表。</param>
        /// <param name="fieldName">外层键选择器。</param>
        /// <param name="fieldName2">内层键选择器。</param>
        /// <param name="loadVal">值选择器。</param>
        /// <returns>两级嵌套字典。</returns>
        public static Dictionary<K1, Dictionary<K2, K3>> groupByKV<K1, K2, K3>(this DataTable dt, Func<DataRow, K1> fieldName, Func<DataRow, K2> fieldName2, Func<DataRow, K3> loadVal)
        {
            Dictionary<K1, Dictionary<K2, K3>> dictionary = new Dictionary<K1, Dictionary<K2, K3>>();
            foreach (DataRow row in dt.Rows)
            {
                var k1 = fieldName(row);
                if (k1 == null)
                {
                    continue;
                }
                var k2 = fieldName2(row);
                if (k2 == null)
                {
                    continue;
                }
                var val = loadVal(row);

                if (!dictionary.ContainsKey(k1))
                {
                    dictionary.Add(k1, new Dictionary<K2, K3>());
                }

                dictionary[k1][k2] = val;
            }

            return dictionary;
        }
        /// <summary>
        /// 按2个字段分组
        /// </summary>
        /// <typeparam name="K3"></typeparam>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldName2"></param>
        /// <param name="loadVal"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string,List<K3>>> groupBy<K3>(this DataTable dt, string fieldName, string fieldName2, Func<DataRow, K3> loadVal) { 
            
            return groupBy<string,string,K3>(dt
                ,(row)=>row.getString(fieldName)
                ,(row)=>row.getString(fieldName2)
                ,loadVal);
        }
        /// <summary>
        /// 按2个字段分组
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldName2"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string,List<DataRow>>> groupBy(this DataTable dt, string fieldName, string fieldName2)
        {

            return groupBy<string, string, DataRow>(dt
                , (row) => row.getString(fieldName)
                , (row) => row.getString(fieldName2)
                , (row)=>row);
        }
    }
}
