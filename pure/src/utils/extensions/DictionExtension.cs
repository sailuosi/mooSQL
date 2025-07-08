using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 集合扩展
    /// </summary>
    public static class DictExtension
    {
        /// <summary>
        /// 安全的获取字典值
        /// </summary>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string getValue(this Dictionary<string, string> map, string key)
        {
            if (key == null) return null;
            if (map.ContainsKey(key))
            {
                return map[key];
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 添加非null成员，非重复成员
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddNotNull<K, T>(this Dictionary<K, T> map, K key, T value)
        {
            if (value == null) { return; }
            if (map.ContainsKey(key))
            {
                map[key] = value;
            }
            else
            {
                map.Add(key, value);
            }
        }
        /// <summary>
        /// 添加非重复成员
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        public static void AddNotRepeat<T>(this List<T> list, T value)
        {
            if (list.Contains(value) == false) list.Add(value);
        }
        /// <summary>
        /// 添加非重复
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        public static void AddNotRepeat<T>(this List<T> list, List<T> value)
        {
            foreach (T v in value) {
                if (list.Contains(v) == false) list.Add(v);
            }
            
        }
        /// <summary>
        /// 包装为可空类型列表，便于后续操作
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
        public static List<R?> WrapNullable<R>(this List<R> val) where R : struct
        {
            var res = new List<R?>(val.Count);
            for (var i = 0; i < val.Count; i++)
            {
                res.Add(val[i]);
            }
            return res;
        }
        /// <summary>
        /// 解包装可空类型列表，便于后续操作
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
        public static List<R> UnWrapNullable<R>(this List<R?> val) where R : struct
        {
            var res = new List<R>();
            for (var i = 0; i < val.Count; i++)
            {
                var v = val[i];
                if (v != null) {
                    res.Add(v.Value);
                }
                
            }
            return res;
        }

        public static void AddNotNull(this Dictionary<string, string> map, string key, string value)
        {
            if (value == null) { return; }
            if (map.ContainsKey(key))
            {
                map[key] = value;
            }
            else
            {
                map.Add(key, value);
            }
        }
        /// <summary>
        /// 拼接
        /// </summary>
        /// <param name="map"></param>
        /// <param name="seprate"></param>
        /// <param name="useValue"></param>
        /// <returns></returns>
        public static string JoinNotNull(this Dictionary<string, string> map, string seprate, bool useValue)
        {
            var res = new StringBuilder();
            foreach (var kv in map)
            {
                if (useValue && kv.Value != null)
                {
                    if (res.Length > 0) { res.Append(seprate); }
                    res.Append(kv.Value);
                }
                else
                {
                    if (res.Length > 0) { res.Append(seprate); }
                    res.Append(kv.Key);
                }
            }
            return res.ToString();

        }
        /// <summary>
        /// 拼接字符串列表，并忽略空白字符串。
        /// </summary>
        /// <param name="list"></param>
        /// <param name="seprate"></param>
        /// <returns></returns>
        public static string JoinNotEmpty(this List<string> list, string seprate)
        {
            var res = new StringBuilder();
            foreach (var li in list)
            {
                if (!string.IsNullOrWhiteSpace(li))
                {
                    if (res.Length > 0) { res.Append(seprate); }
                    res.Append(li);
                }
            }
            return res.ToString();

        }


        /// <summary>
        /// 根据列的数据库列类型，转换到对应的数据格式
        /// </summary>
        /// <param name="val"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static object shapeDataType(object val, Type dataType)
        {
            var res = new object();
            try
            {
                switch (dataType.Name)
                {
                    case "String":
                        res = val.ToString();
                        break;
                    case "DateTime":
                        //res = Convert.ToDateTime(val);
                        var outd = new DateTime();
                        if (DateTime.TryParse(val.ToString(), out outd))
                        {
                            res = outd;
                        }
                        else
                        {
                            res = DateTime.Parse("1900-01-01 00:00:00.000");
                        }
                        break;
                    case "Guid":
                        res = new Guid(val.ToString());
                        break;
                    case "Boolean":
                        var varStr = val.ToString();
                        if (varStr == "1" || varStr == "true" || varStr == "True")
                        {
                            res = true;
                        }
                        else if (varStr == "0" || varStr == "false" || varStr == "False")
                        {
                            res = false;
                        }
                        else
                        {
                            res = Convert.ToBoolean(val);
                        }
                        break;
                    case "Int32":
                        res = Convert.ToInt32(val);
                        break;
                    default:
                        res = Convert.ChangeType(val, dataType);
                        break;
                }
            }
            catch
            {
                res = DBNull.Value;
            }
            if (res == null)
            {
                res = DBNull.Value;
            }
            return res;
        }

        internal static HashSet<T> AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (var item in items)
                hashSet.Add(item);
            return hashSet;
        }
        internal static bool HasValue(this IEnumerable<object> thisValue)
        {
            if (thisValue == null || thisValue.Count() == 0)
            {
                return false;
            }
            return true;
        }
    }
}
