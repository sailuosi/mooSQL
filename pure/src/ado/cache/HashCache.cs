
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace mooSQL.data
{
    public class HashCache:ISooCache
    {
        public HashCache() //CLR调用  整个进程执行且只执行一次
        {
            Task.Run(() => //
            {
                while (true) //死循环来判断
                {
                    try
                    {
                        List<string> delKeyList = new List<string>();

                        lock (obj_Lock)
                        {
                            foreach (string key in cacheHolder.Keys)
                            {
                                DataModel model = cacheHolder[key] as DataModel;
                                if (model == null) {
                                    continue;
                                }
                                if (model.Deadline < DateTime.Now && model.ObsloteType != ObsloteType.Never)
                                {
                                    delKeyList.Add(key);
                                }
                            }
                        }
                        delKeyList.ForEach(key => Remove(key));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            });
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
                cacheHolder.Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Never
                });
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
                cacheHolder.Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Absolutely,
                    Deadline = DateTime.Now.AddSeconds(timeOutSecond)
                }); ;
        }

        public void Add(string key, object value, TimeSpan durtion)
        {
            lock (obj_Lock)
                cacheHolder.Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Relative,
                    Deadline = DateTime.Now.Add(durtion),
                    Duraton = durtion
                }); ; ;
        }


        //清楚所有缓存，殃及池鱼！
        public void RemoveAll()
        {
            lock (obj_Lock)
                cacheHolder.Clear();//字典中的所有内容全部被清理到
        }

        public void Remove(string key)
        {
            lock (obj_Lock)
                cacheHolder.Remove(key);
        }

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

        public T Get<T>(string key)
        {
            var val = cacheHolder[key] as DataModel;
            if (val == null) {
                return default(T);
            }
            return (T)val.Value ;
        }

        public bool ContainsKey(string key)
        {
            if (cacheHolder.ContainsKey(key))
            {
                DataModel model = cacheHolder[key] as DataModel;
                if(model==null) return false;
                if (model.ObsloteType == ObsloteType.Never)
                {
                    return true;
                }
                else if (model.Deadline < DateTime.Now) //
                {
                    lock (obj_Lock)
                    {

                        cacheHolder.Remove(key);
                        return false;
                    }

                }
                else
                {
                    if (model.ObsloteType == ObsloteType.Relative)
                    {
                        model.Deadline = DateTime.Now.Add(model.Duraton);
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

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

        public IEnumerable<string> GetKeys()
        {
            var keys= cacheHolder.Keys;
            var res =new List<string>();
            foreach (var key in keys)
            {
                res.Add(key.ToString());
            }
            return res;
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
        public DictionaryCache() //CLR调用  整个进程执行且只执行一次
        {
            Task.Run(() => //
            {
                while (true) //死循环来判断
                {
                    try
                    {
                        List<string> delKeyList = new List<string>();

                        lock (obj_Lock)
                        {
                            foreach (var key in CustomCacheDictionary.Keys)
                            {
                                DataModel model = CustomCacheDictionary[key];
                                if (model.Deadline < DateTime.Now && model.ObsloteType != ObsloteType.Never)
                                {
                                    delKeyList.Add(key);
                                }
                            }
                        }
                        delKeyList.ForEach(key => Remove(key));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            });
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
                CustomCacheDictionary.Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Never
                });
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
                CustomCacheDictionary.Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Absolutely,
                    Deadline = DateTime.Now.AddSeconds(timeOutSecond)
                }); ;
        }

        public void Add(string key, object value, TimeSpan durtion)
        {
            lock (obj_Lock)
                CustomCacheDictionary.Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Relative,
                    Deadline = DateTime.Now.Add(durtion),
                    Duraton = durtion
                }); ; ;
        }


        //清楚所有缓存，殃及池鱼！
        public void RemoveAll()
        {
            lock (obj_Lock)
                CustomCacheDictionary.Clear();//字典中的所有内容全部被清理到
        }

        public void Remove(string key)
        {
            lock (obj_Lock)
                CustomCacheDictionary.Remove(key);
        }

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

        public T Get<T>(string key)
        {
            return (T)(CustomCacheDictionary[key]).Value;
        }

        public bool ContainsKey(string key)
        {
            if (CustomCacheDictionary.ContainsKey(key))
            {
                DataModel model = CustomCacheDictionary[key];
                if (model.ObsloteType == ObsloteType.Never)
                {
                    return true;
                }
                else if (model.Deadline < DateTime.Now) //
                {
                    lock (obj_Lock)
                    {

                        CustomCacheDictionary.Remove(key);
                        return false;
                    }

                }
                else
                {
                    if (model.ObsloteType == ObsloteType.Relative)
                    {
                        model.Deadline = DateTime.Now.Add(model.Duraton);
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

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

        public IEnumerable<string> GetKeys()
        {
            return CustomCacheDictionary.Keys;
        }
    }
    /// <summary>
    /// 线程安全cache
    /// </summary>
    public class DictionaryCacheSafe:ISooCache
    {

        public DictionaryCacheSafe() //
        {
            Task.Run(() => //
            {
                while (true) //死循环来判断
                {
                    try
                    {
                        //Thread.Sleep(60 * 1000 * 10); //十分钟后开始清理缓存
                        List<string> delKeyList = new List<string>();
                        foreach (var key in CustomCacheDictionary.Keys)
                        {
                            DataModel model = CustomCacheDictionary[key];
                            if (model.Deadline < DateTime.Now && model.ObsloteType != ObsloteType.Never) //
                            {
                                delKeyList.Add(key);
                            }
                        }
                        delKeyList.ForEach(key => Remove(key));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            });

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
            CustomCacheDictionary.TryAdd(key, new DataModel()
            {
                Value = value,
                ObsloteType = ObsloteType.Never
            });
        }

        /// <summary>
        /// 绝对过期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeOutSecond"></param>
        public void Add<V>(string key, V value, int timeOutSecond) //3000
        {
            CustomCacheDictionary.TryAdd(key, new DataModel()
            {
                Value = value,
                ObsloteType = ObsloteType.Absolutely,
                Deadline = DateTime.Now.AddSeconds(timeOutSecond)
            }); ;
        }

        public void Add<V>(string key, V value, TimeSpan durtion)
        {
            CustomCacheDictionary.TryAdd(key, new DataModel()
            {
                Value = value,
                ObsloteType = ObsloteType.Relative,
                Deadline = DateTime.Now.Add(durtion),
                Duraton = durtion
            }); ; ;
        }


        //清楚所有缓存，殃及池鱼！
        public void RemoveAll()
        {
            CustomCacheDictionary.Clear();//字典中的所有内容全部被清理到
        }

        public void Remove(string key)
        {
            DataModel data = null;
            CustomCacheDictionary.TryRemove(key, out data);
        }


        public T Get<T>(string key)
        {
            return (T)(CustomCacheDictionary[key]).Value;
        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {

            if (CustomCacheDictionary.ContainsKey(key))
            {
                DataModel model = CustomCacheDictionary[key];
                if (model.ObsloteType == ObsloteType.Never)
                {
                    return true;
                }
                else if (model.Deadline < DateTime.Now) //
                {
                    DataModel data = null;
                    CustomCacheDictionary.TryRemove(key, out data);
                    return false;
                }
                else
                {
                    if (model.ObsloteType == ObsloteType.Relative)
                    {
                        model.Deadline = DateTime.Now.Add(model.Duraton);
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

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

    public enum ObsloteType
    {
        Never,
        Absolutely,
        Relative
    }

    /// <summary>
    /// 解决性能问题
    /// </summary>
    public class CustomCacheNewproblem
    {

        private static List<Dictionary<string, DataModel>> dicCacheList = new List<Dictionary<string, DataModel>>();
        private static List<object> lockList = new List<object>();

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


            Task.Run(() => //
            {
                while (true) //死循环来判断
                {
                    try
                    {

                        for (int i = 0; i < CupNum; i++)
                        {
                            lock (lockList[i])
                            {
                                //Thread.Sleep(60 * 1000 * 10); //十分钟后开始清理缓存
                                List<string> delKeyList = new List<string>();
                                foreach (var key in dicCacheList[i].Keys)
                                {
                                    DataModel model = dicCacheList[i][key];
                                    if (model.Deadline < DateTime.Now && model.ObsloteType != ObsloteType.Never) //
                                    {
                                        delKeyList.Add(key);
                                    }
                                }
                                delKeyList.ForEach(key => dicCacheList[i].Remove(key));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            });

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
                dicCacheList[index].Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Never
                });
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
                dicCacheList[index].Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Absolutely,
                    Deadline = DateTime.Now.AddSeconds(timeOutSecond)
                }); ;
        }

        public static void Add(string key, object value, TimeSpan durtion)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;
            lock (lockList[index])
                dicCacheList[index].Add(key, new DataModel()
                {
                    Value = value,
                    ObsloteType = ObsloteType.Relative,
                    Deadline = DateTime.Now.Add(durtion),
                    Duraton = durtion
                }); ; ;
        }


        //清楚所有缓存，殃及池鱼！
        public static void RemoveAll()
        {
            for (int i = 0; i < CupNum; i++)
            {
                dicCacheList[i].Clear();
            }
        }

        public static void Remove(string key)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;

            if (dicCacheList[index].ContainsKey(key))
            {
                dicCacheList[index].Remove(key);
            }
        }


        public static T Get<T>(string key)
        {
            int hash = key.GetHashCode() * (-1); //只要字符串变，hash值不变！
            int index = hash % CupNum;

            return (T)(dicCacheList[index][key]).Value;
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
            if (dicCacheList[index].ContainsKey(key))
            {
                DataModel model = dicCacheList[index][key];
                if (model.ObsloteType == ObsloteType.Never)
                {
                    return true;
                }
                else if (model.Deadline < DateTime.Now) //
                {
                    dicCacheList[index].Remove(key);
                    return false;
                }
                else
                {
                    if (model.ObsloteType == ObsloteType.Relative)
                    {
                        model.Deadline = DateTime.Now.Add(model.Duraton);
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

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
