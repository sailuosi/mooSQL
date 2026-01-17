// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 权限用到的一些快捷语法糖
    /// </summary>
    public static class MooAuthExtension
    {
        /// <summary>
        /// 将对象映射为一个另一个列表
        /// </summary>
        /// <returns></returns>
        public static List<R> map<T, R>(this IEnumerable<T> list, Func<T, R> onfilter)
        {
            var res = new List<R>();
            foreach (T item in list)
            {
                var t = onfilter(item);
                if (!res.Contains(t))
                {
                    if (typeof(T) == typeof(string))
                    {
                        var strT = item as string;
                        if (string.IsNullOrWhiteSpace(strT))
                        {
                            continue;
                        }
                    }
                    res.Add(t);
                }
            }
            return res;
        }
        /// <summary>
        /// 过滤列表，返回一个新的列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="onfilter"></param>
        /// <returns></returns>
        public static List<T> filter<T>(this IEnumerable<T> list, Func<T, bool> onfilter)
        {
            var res = new List<T>();
            foreach (T item in list)
            {
                var t = onfilter(item);
                if (t)
                {
                    res.Add(item);
                }
            }
            return res;
        }
        /// <summary>
        /// 写入指定集合，并返回该集合，便于在查询中结果后直接链式写入到目标集合中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static ICollection<T> writeTo<T>(this IEnumerable<T> list, ICollection<T> target)
        {

            foreach (T item in target)
            {
                if (target.Contains(item)==false)
                {
                    target.Add(item);
                }
            }
            return target;
        }
        /// <summary>
        /// 将列表对象映射为一个字典
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="list"></param>
        /// <param name="keySelector"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        public static Dictionary<K,V> mapToDic<T, K,V>(this IEnumerable<T> list, Func<T, K> keySelector, Func<T, V> valueSelector)
            where K:notnull
        {
            var res = new Dictionary<K,V>();

            bool isStrlist = typeof(T) == typeof(string);

            foreach (T item in list)
            {
                if (isStrlist)
                {
                    var strT = item as string;
                    if (string.IsNullOrWhiteSpace(strT))
                    {
                        continue;
                    }
                }
                var key =keySelector(item);
                var value =valueSelector(item);
                if (!res.ContainsKey(key))
                {
                    res.Add(key,value);
                }
            }
            return res;
        }
    }
}