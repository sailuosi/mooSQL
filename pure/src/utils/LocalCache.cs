
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    public class LocalCache
    {
        private static MemoryCache cache = MemoryCache.Default;

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
        /// 删除全部缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
