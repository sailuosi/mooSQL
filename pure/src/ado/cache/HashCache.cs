
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 HashCache。
    /// 过期项在 Get/ContainsKey 时惰性清理，不启动后台扫描线程。
    /// </summary>
    public class HashCache:ISooCache
    {
        /// <summary>
        /// 初始化 HashCache。
        /// </summary>
        public HashCache()
        {
        }

        /// <summary>
        /// static:不会被Gc回收；
        /// Private：不让外部访问他 
        /// </summary>
        private Hashtable cacheHolder = new Hashtable();

        private readonly object obj_Lock = new object();


        /// <summary>
        /// 默认你是不过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add<V>(string key, V value)
        {
            lock (obj_Lock)
                cacheHolder[key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Never
                };
        }

        /// <summary>
        /// 绝对过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeOutSecond"></param>
        public void Add<V>(string key, V value, int timeOutSecond) //3000
        {
            lock (obj_Lock)
                cacheHolder[key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Absolutely,
                    Deadline = DateTime.Now.AddSeconds(timeOutSecond)
                };
        }

        /// <summary>
        /// Add 方法。
        /// </summary>
        public void Add(string key, object value, TimeSpan durtion)
        {
            lock (obj_Lock)
                cacheHolder[key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Relative,
                    Deadline = DateTime.Now.Add(durtion),
                    Duraton = durtion
                };
        }


        //清楚所有缓存，殃及池鱼！
        /// <summary>
        /// 移除All。
        /// </summary>
        public void RemoveAll()
        {
            lock (obj_Lock)
                cacheHolder.Clear();//字典中的所有内容全部被清理到
        }

        /// <summary>
        /// Remove 方法。
        /// </summary>
        public void Remove(string key)
        {
            lock (obj_Lock)
                cacheHolder.Remove(key);
        }

        /// <summary>
        /// 移除Condition。
        /// </summary>
        public void RemoveCondition(Func<string, bool> func)
        {
            List<string> keyList = new List<string>();
            lock (obj_Lock)
                foreach (string key in cacheHolder.Keys)
                {
                    if (func.Invoke(key))
                    {
                        keyList.Add(key);
                    }
                }
            keyList.ForEach(s => Remove(s));
        }

        /// <summary>
        /// 按键获取缓存项（惰性过期）。
        /// </summary>
        public T Get<T>(string key)
        {
            DataModel model;
            lock (obj_Lock)
            {
                model = cacheHolder[key] as DataModel;
                if (model == null)
                    return default(T);

                if (model.ObsloteType != ObsloteType.Never && model.Deadline < DateTime.Now)
                {
                    cacheHolder.Remove(key);
                    return default(T);
                }

                if (model.ObsloteType == ObsloteType.Relative)
                    model.Deadline = DateTime.Now.Add(model.Duraton);

                return (T)model.Value;
            }
        }

        /// <summary>
        /// ContainsKey 方法（返回 bool）。
        /// </summary>
        public bool ContainsKey(string key)
        {
            lock (obj_Lock)
            {
                if (!cacheHolder.ContainsKey(key))
                    return false;

                DataModel model = cacheHolder[key] as DataModel;
                if (model == null)
                    return false;

                if (model.ObsloteType == ObsloteType.Never)
                    return true;

                if (model.Deadline < DateTime.Now)
                {
                    cacheHolder.Remove(key);
                    return false;
                }

                if (model.ObsloteType == ObsloteType.Relative)
                    model.Deadline = DateTime.Now.Add(model.Duraton);

                return true;
            }
        }

        /// <summary>
        /// 获取缓存项，未命中时通过工厂委托创建并写入。
        /// </summary>
        public T GetT<T>(string key, Func<T> func)
        {
            T t = default(T);
            if (!ContainsKey(key))
            {
                t = func.Invoke();
                Add(key, t);
            }
            else
            {
                t = Get<T>(key);
            }
            return t;
        }

        /// <summary>
        /// 获取Keys。
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            lock (obj_Lock)
            {
                var res = new List<string>(cacheHolder.Count);
                foreach (var key in cacheHolder.Keys)
                {
                    res.Add(key.ToString());
                }
                return res;
            }
        }
    }
    /// <summary>
    /// 永不过期：当前就是
    /// 绝对过期：过了多长时间以后，就过期了 就不能用了
    /// 滑动过期：设定好过期时间后，如果在有效期内使用过，就往后滑动
    /// 1.Value;数据；
    /// 2.过期时间点：
    /// 3.滑动时间
    /// 普通cache
    /// </summary>
    public class DictionaryCache:ISooCache
    {
        /// <summary>
        /// 初始化 DictionaryCache。
        /// </summary>
        public DictionaryCache()
        {
        }

        /// <summary>
        /// static:不会被Gc回收；
        /// Private：不让外部访问他 
        /// </summary>
        private Dictionary<string, DataModel> CustomCacheDictionary = new Dictionary<string, DataModel>();

        private readonly object obj_Lock = new object();


        /// <summary>
        /// 默认你是不过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add<V>(string key, V value)
        {
            lock (obj_Lock)
                CustomCacheDictionary[key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Never
                };
        }

        /// <summary>
        /// 绝对过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeOutSecond"></param>
        public void Add<V>(string key, V value, int timeOutSecond) //3000
        {
            lock (obj_Lock)
                CustomCacheDictionary[key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Absolutely,
                    Deadline = DateTime.Now.AddSeconds(timeOutSecond)
                };
        }

        /// <summary>
        /// Add 方法。
        /// </summary>
        public void Add(string key, object value, TimeSpan durtion)
        {
            lock (obj_Lock)
                CustomCacheDictionary[key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Relative,
                    Deadline = DateTime.Now.Add(durtion),
                    Duraton = durtion
                };
        }


        //清楚所有缓存，殃及池鱼！
        /// <summary>
        /// 移除All。
        /// </summary>
        public void RemoveAll()
        {
            lock (obj_Lock)
                CustomCacheDictionary.Clear();//字典中的所有内容全部被清理到
        }

        /// <summary>
        /// Remove 方法。
        /// </summary>
        public void Remove(string key)
        {
            lock (obj_Lock)
                CustomCacheDictionary.Remove(key);
        }

        /// <summary>
        /// 移除Condition。
        /// </summary>
        public void RemoveCondition(Func<string, bool> func)
        {
            List<string> keyList = new List<string>();
            lock (obj_Lock)
                foreach (var key in CustomCacheDictionary.Keys)
                {
                    if (func.Invoke(key))
                    {
                        keyList.Add(key);
                    }
                }
            keyList.ForEach(s => Remove(s));
        }

        /// <summary>
        /// 按键获取缓存项（惰性过期）。
        /// </summary>
        public T Get<T>(string key)
        {
            lock (obj_Lock)
            {
                DataModel model;
                if (!CustomCacheDictionary.TryGetValue(key, out model) || model == null)
                    return default(T);

                if (model.ObsloteType != ObsloteType.Never && model.Deadline < DateTime.Now)
                {
                    CustomCacheDictionary.Remove(key);
                    return default(T);
                }

                if (model.ObsloteType == ObsloteType.Relative)
                    model.Deadline = DateTime.Now.Add(model.Duraton);

                return (T)model.Value;
            }
        }

        /// <summary>
        /// ContainsKey 方法（返回 bool）。
        /// </summary>
        public bool ContainsKey(string key)
        {
            lock (obj_Lock)
            {
                DataModel model;
                if (!CustomCacheDictionary.TryGetValue(key, out model) || model == null)
                    return false;

                if (model.ObsloteType == ObsloteType.Never)
                    return true;

                if (model.Deadline < DateTime.Now)
                {
                    CustomCacheDictionary.Remove(key);
                    return false;
                }

                if (model.ObsloteType == ObsloteType.Relative)
                    model.Deadline = DateTime.Now.Add(model.Duraton);

                return true;
            }
        }

        /// <summary>
        /// 获取缓存项，未命中时通过工厂委托创建并写入。
        /// </summary>
        public T GetT<T>(string key, Func<T> func)
        {
            T t = default(T);
            if (!ContainsKey(key))
            {
                t = func.Invoke();
                Add(key, t);
            }
            else
            {
                t = Get<T>(key);
            }
            return t;
        }

        /// <summary>
        /// 获取Keys。
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            lock (obj_Lock)
                return new List<string>(CustomCacheDictionary.Keys);
        }
    }
    /// <summary>
    /// 线程安全cache
    /// </summary>
    public class DictionaryCacheSafe:ISooCache
    {

        /// <summary>
        /// 初始化 DictionaryCacheSafe。
        /// </summary>
        public DictionaryCacheSafe()
        {
        }

        /// <summary>
        /// static:不会被Gc回收；
        /// Private：不让外部访问他 
        /// 
        /// 线程安全字典
        /// </summary>
        private  ConcurrentDictionary<string, DataModel> CustomCacheDictionary = new ConcurrentDictionary<string, DataModel>();

        /// <summary>
        /// 默认你是不过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public  void Add<V>(string key, V value)
        {
            CustomCacheDictionary[key] = new DataModel()
            {
                Value = value,
                ObsloteType = ObsloteType.Never
            };
        }

        /// <summary>
        /// 绝对过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeOutSecond"></param>
        public void Add<V>(string key, V value, int timeOutSecond) //3000
        {
            CustomCacheDictionary[key] = new DataModel()
            {
                Value = value,
                ObsloteType = ObsloteType.Absolutely,
                Deadline = DateTime.Now.AddSeconds(timeOutSecond)
            };
        }

        /// <summary>
        /// 写入带过期时间的缓存项。
        /// </summary>
        public void Add<V>(string key, V value, TimeSpan durtion)
        {
            CustomCacheDictionary[key] = new DataModel()
            {
                Value = value,
                ObsloteType = ObsloteType.Relative,
                Deadline = DateTime.Now.Add(durtion),
                Duraton = durtion
            };
        }


        //清楚所有缓存，殃及池鱼！
        /// <summary>
        /// 移除All。
        /// </summary>
        public void RemoveAll()
        {
            CustomCacheDictionary.Clear();//字典中的所有内容全部被清理到
        }

        /// <summary>
        /// Remove 方法。
        /// </summary>
        public void Remove(string key)
        {
            DataModel data = null;
            CustomCacheDictionary.TryRemove(key, out data);
        }


        /// <summary>
        /// 按键获取缓存项（惰性过期）。
        /// </summary>
        public T Get<T>(string key)
        {
            DataModel model;
            if (!CustomCacheDictionary.TryGetValue(key, out model) || model == null)
                return default(T);

            if (model.ObsloteType != ObsloteType.Never && model.Deadline < DateTime.Now)
            {
                DataModel removed;
                CustomCacheDictionary.TryRemove(key, out removed);
                return default(T);
            }

            if (model.ObsloteType == ObsloteType.Relative)
                model.Deadline = DateTime.Now.Add(model.Duraton);

            return (T)model.Value;
        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {

            DataModel model;
            if (!CustomCacheDictionary.TryGetValue(key, out model) || model == null)
                return false;

            if (model.ObsloteType == ObsloteType.Never)
                return true;

            if (model.Deadline < DateTime.Now)
            {
                DataModel data = null;
                CustomCacheDictionary.TryRemove(key, out data);
                return false;
            }

            if (model.ObsloteType == ObsloteType.Relative)
                model.Deadline = DateTime.Now.Add(model.Duraton);

            return true;
        }

        /// <summary>
        /// 获取缓存项，未命中时通过工厂委托创建并写入。
        /// </summary>
        public T GetT<T>(string key, Func<T> func)
        {
            T t = default(T);
            if (!ContainsKey(key))
            {
                t = func.Invoke();
                Add(key, t);
            }
            else
            {
                t = Get<T>(key);
            }
            return t;
        }

        /// <summary>
        /// 获取Keys。
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            return CustomCacheDictionary.Keys;
        }
    }

    internal class DataModel
    {
        public object Value { get; set; }

        public ObsloteType ObsloteType { get; set; }

        public DateTime Deadline { get; set; }

        public TimeSpan Duraton { get; set; }
    }

    /// <summary>
    /// 枚举 ObsloteType。
    /// </summary>
    public enum ObsloteType
    {
        /// <summary>
        /// 类型 CustomCacheNewproblem。
        /// </summary>
        Never,
        /// <summary>
        /// 类型 CustomCacheNewproblem。
        /// </summary>
        Absolutely,
        /// <summary>
        /// 类型 CustomCacheNewproblem。
        /// </summary>
        Relative
    }

    /// <summary>
    /// 解决性能问题
    /// </summary>
    public class CustomCacheNewproblem
    {

        private static List<Dictionary<string, DataModel>> dicCacheList = new List<Dictionary<string, DataModel>>();
        private static List<object> lockList = new List<object>();

        /// <summary>
        /// 字段 CupNum（int）。
        /// </summary>
        public static int CupNum = 0;
        static CustomCacheNewproblem()
        {
            CupNum = 3;//模拟获取获取CPU片数  
            //动态生成字典
            for (int i = 0; i < CupNum; i++)
            {
                dicCacheList.Add(new Dictionary<string, DataModel>()); //CPU 有几片 就来几个字典
                lockList.Add(new object());//没个字典对应一个锁
            }
        }

        /// <summary>
        /// 默认你是不过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Add(string key, object value)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;
            lock (lockList[index])
                dicCacheList[index][key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Never
                };
        }

        /// <summary>
        /// 绝对过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeOutSecond"></param>
        public static void Add(string key, object value, int timeOutSecond) //3000
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;
            lock (lockList[index])
                dicCacheList[index][key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Absolutely,
                    Deadline = DateTime.Now.AddSeconds(timeOutSecond)
                };
        }

        /// <summary>
        /// Add 方法。
        /// </summary>
        public static void Add(string key, object value, TimeSpan durtion)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;
            lock (lockList[index])
                dicCacheList[index][key] = new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Relative,
                    Deadline = DateTime.Now.Add(durtion),
                    Duraton = durtion
                };
        }


        //清楚所有缓存，殃及池鱼！
        /// <summary>
        /// 移除All。
        /// </summary>
        public static void RemoveAll()
        {
            for (int i = 0; i < CupNum; i++)
            {
                lock (lockList[i])
                    dicCacheList[i].Clear();
            }
        }

        /// <summary>
        /// Remove 方法。
        /// </summary>
        public static void Remove(string key)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;

            lock (lockList[index])
            {
                if (dicCacheList[index].ContainsKey(key))
                {
                    dicCacheList[index].Remove(key);
                }
            }
        }


        /// <summary>
        /// 按键获取缓存项（惰性过期）。
        /// </summary>
        public static T Get<T>(string key)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;

            lock (lockList[index])
            {
                DataModel model;
                if (!dicCacheList[index].TryGetValue(key, out model) || model == null)
                    return default(T);

                if (model.ObsloteType != ObsloteType.Never && model.Deadline < DateTime.Now)
                {
                    dicCacheList[index].Remove(key);
                    return default(T);
                }

                if (model.ObsloteType == ObsloteType.Relative)
                    model.Deadline = DateTime.Now.Add(model.Duraton);

                return (T)model.Value;
            }
        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exists(string key)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;
            lock (lockList[index])
            {
                DataModel model;
                if (!dicCacheList[index].TryGetValue(key, out model) || model == null)
                    return false;

                if (model.ObsloteType == ObsloteType.Never)
                    return true;

                if (model.Deadline < DateTime.Now)
                {
                    dicCacheList[index].Remove(key);
                    return false;
                }

                if (model.ObsloteType == ObsloteType.Relative)
                    model.Deadline = DateTime.Now.Add(model.Duraton);

                return true;
            }
        }

        /// <summary>
        /// 获取缓存项，未命中时通过工厂委托创建并写入。
        /// </summary>
        public static T GetT<T>(string key, Func<T> func)
        {
            T t = default(T);
            if (!Exists(key))
            {
                t = func.Invoke();
                Add(key, t);
            }
            else
            {
                t = Get<T>(key);
            }
            return t;
        }
    }
}
