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
            var seen = new HashSet<R>();
            foreach (T item in list)
            {
                var t = onfilter(item);
                if (!seen.Contains(t))
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
                    seen.Add(t);
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
        /// 对序列做类 fold 归约（跳过首元素作为种子，再逐元素累积）。
        /// </summary>
        public static R reduece<T,R>(this IEnumerable<T> list, Func<R,T, R> doreduce)
        {
            T pre ;
            bool isFirst = true;
            R res=default(R);
            foreach (T item in list)
            {
                if (isFirst) { 
                    pre = item;
                    isFirst = false;
                    continue;
                }

                 res=doreduce(res, item);

            }
            return res;
        }

        /// <summary>
        /// 对可空 int 列求和（忽略 null）。
        /// </summary>
        public static int sum<T>(this IEnumerable<T> list, Func<T, int?> doselect)
        {
            int r = 0;
            foreach (var li in list) {
                var ri = doselect(li);
                if (ri.HasValue) {
                    r += ri.Value;
                }
            }
            return r;
        }
        /// <summary>
        /// 按条件级数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="doselect"></param>
        /// <returns></returns>
        public static int count<T>(this IEnumerable<T> list, Func<T, bool> doselect)
        {
            int r = 0;
            foreach (var li in list)
            {
                var ri = doselect(li);
                if (ri)
                {
                    r ++;
                }
            }
            return r;
        }
        /// <summary>
        /// 对可空 double 列求和（忽略 null）。
        /// </summary>
        public static double sum<T>(this IEnumerable<T> list, Func<T, double?> doselect)
        {
            double r = 0;
            foreach (var li in list)
            {
                var ri = doselect(li);
                if (ri.HasValue)
                {
                    r += ri.Value;
                }
            }
            return r;
        }

        /// <summary>
        /// 对可空 decimal 列求和（忽略 null）。
        /// </summary>
        public static decimal sum<T>(this IEnumerable<T> list, Func<T, decimal?> doselect)
        {
            decimal r = 0;
            foreach (var li in list)
            {
                var ri = doselect(li);
                if (ri.HasValue)
                {
                    r += ri.Value;
                }
            }
            return r;
        }

        /// <summary>
        /// 对可空 float 列求和（忽略 null）。
        /// </summary>
        public static float sum<T>(this IEnumerable<T> list, Func<T, float?> doselect)
        {
            float r = 0;
            foreach (var li in list)
            {
                var ri = doselect(li);
                if (ri.HasValue)
                {
                    r += ri.Value;
                }
            }
            return r;
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

            if (list == null || target == null)
            {
                return target;
            }

            foreach (T item in list)
            {
                if (target.Contains(item) == false)
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
                if (key == null) { 
                    continue;
                }
                if (!res.ContainsKey(key))
                {
                    res.Add(key,value);
                }
            }
            return res;
        }
    }
}