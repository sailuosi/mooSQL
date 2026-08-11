
using System;
using System.Collections.Generic;


namespace mooSQL.data
{
    /// <summary>
    /// 内置缓存实现的默认约定。
    /// </summary>
    public static class SooCacheDefaults
    {
        /// <summary>无显式 TTL 时的默认绝对过期：12 小时。</summary>
        public static readonly TimeSpan Expiration = TimeSpan.FromHours(12);

        /// <summary>默认过期秒数（12 小时）。</summary>
        public const int ExpirationSeconds = 12 * 60 * 60;
    }

    /// <summary>
    /// 缓存标准接口
    /// </summary>
    public interface ISooCache
    {
        /// <summary>
        /// 添加缓存数据（未指定 TTL 时由实现类的默认过期策略决定）。
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
        /// 获取或创建：命中则返回；未命中则调用工厂并写入（使用实现类默认过期）。
        /// </summary>
        V GetOrCreate<V>(string key, Func<V> factory);
        /// <summary>
        /// 获取或创建：命中则返回；未命中则调用工厂并以绝对秒数过期写入。
        /// </summary>
        V GetOrCreate<V>(string key, Func<V> factory, int cacheDurationInSeconds);
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
