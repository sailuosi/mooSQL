using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 存放成员属性部分
    /// </summary>
    public partial class PackUp
    {
        // use Hashtable to get free lockless reading
        private readonly Hashtable _typeMaps = new Hashtable();

        /// <summary>
        /// 为类型反序列化器设置自定义映射
        /// </summary>
        /// <param name="type">要覆盖的实体类型</param>
        /// <param name="map">映射规则实现，null 表示移除自定义映射</param>
        public void SetTypeMap(Type type, ITypeMap map)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (map is null || map is DefaultTypeMap)
            {
                lock (_typeMaps)
                {
                    _typeMaps.Remove(type);
                }
            }
            else
            {
                lock (_typeMaps)
                {
                    _typeMaps[type] = map;
                }
            }

            MapperCache.PurgeQueryCacheByType(type);
        }

        /// <summary>
        /// 获取给定 <see cref="Type"/> 的类型映射。
        /// </summary>
        /// <param name="type">要获取映射的类型。</param>
        /// <returns>类型映射实现，如果没有覆盖则返回 DefaultTypeMap 实例</returns>
        public ITypeMap GetTypeMap(Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            var map = (ITypeMap)_typeMaps[type];
            if (map is null)
            {
                lock (_typeMaps)
                {   // double-checked; store this to avoid reflection next time we see this type
                    // since multiple queries commonly use the same domain-entity/DTO/view-model type
                    map = (ITypeMap)_typeMaps[type];

                    if (map is null)
                    {
                        map = GetTypeMapProvider(type);
                        _typeMaps[type] = map;
                    }
                }
            }
            return map;
        }

        /// <summary>
        /// 获取给定类型的类型映射
        /// </summary>
        /// <returns>类型映射实例，默认是创建 DefaultTypeMap 的新实例</returns>
#pragma warning disable CA2211 // Non-constant fields should not be visible - I agree with you, but we can't do that until we break the API
        public ITypeMap GetTypeMapProvider(Type type) {
            return new DefaultTypeMap(type, this._client);
        } 
#pragma warning restore CA2211 // Non-constant fields should not be visible
    }
}
