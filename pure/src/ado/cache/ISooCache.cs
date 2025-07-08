
using System.Collections.Generic;


namespace mooSQL.data
{
    /// <summary>
    /// 缓存标准接口
    /// </summary>
    public interface ISooCache
    {
        /// <summary>
        /// 添加缓存数据
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Add<V>(string key, V value);
        /// <summary>
        /// 添加缓存数据并设置过期时间
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheDurationInSeconds"></param>
        void Add<V>(string key, V value, int cacheDurationInSeconds);
        /// <summary>
        /// 判断是否存在缓存数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool ContainsKey(string key);
        /// <summary>
        /// 获取缓存数据
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        V Get<V>(string key);
        /// <summary>
        /// 获取所有缓存键
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetKeys();
        /// <summary>
        /// 移除缓存数据
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key);
    }


}
