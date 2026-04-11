
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 基于 <see cref="MemoryCache"/> 的进程内键值缓存工具。
    /// </summary>
    public class LocalCache
    {
        private static MemoryCache cache = MemoryCache.Default;

        /// <summary>
        /// 按键读取缓存并转换为 <typeparamref name="T"/>；缺失或类型不匹配时返回 default。
        /// </summary>
        /// <typeparam name="T">期望类型。</typeparam>
        /// <param name="key">缓存键。</param>
        /// <returns>缓存值或 default(T)。</returns>
        public static T Get<T>(string key){
            var val= cache.Get(key);
            if (val == null) {
                return default(T);
            }
            if (val is T) {
                return (T)val;
            }
            return default(T);
        }

        /// <summary>
        /// 按键读取缓存为 object；无项时返回 null。
        /// </summary>
        /// <param name="key">缓存键。</param>
        /// <returns>缓存对象。</returns>
        public static object Get(string key)
        {
            var val = cache.Get(key);
            return val;
        }
        /// <summary>
        /// 默认缓存24小时，单位秒
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="minites"></param>
        public static void Set<T>(string key, T val,int minites=86400) {
            cache.Set(key, val, DateTime.Now.AddMinutes(minites));
        }

        /// <summary>
        /// 移除指定键的缓存项。
        /// </summary>
        /// <param name="key">缓存键。</param>
        public static void Remove(string key) { 
            cache.Remove(key);
        }

        /// <summary>
        /// 模糊搜索
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static List<string> Find(string key) {
            var res= new List<string>();
            foreach (var kv in cache) {
                if (Regex.IsMatch(kv.Key, key)) { 
                    res.Add(kv.Key);
                }
            }
            return res;
        }
        /// <summary>
        /// 删除全部缓存项。
        /// </summary>
        /// <returns>已删除的项数量。</returns>
        public static int RemoveAll()
        {
            var res = 0;
            foreach (var kv in cache)
            {
                cache.Remove(kv.Key);
                res++;
            }
            return res;
        }

        /// <summary>
        /// 模糊删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static int RemoveLike(string key)
        {
            var res = 0;
            foreach (var kv in cache)
            {
                if (Regex.IsMatch(kv.Key, key))
                {
                    cache.Remove(kv.Key);
                    res++;
                }
            }
            return res;
        }
    }
}
