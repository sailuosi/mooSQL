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
    public partial class Deserializer
    {
        // use Hashtable to get free lockless reading
        private readonly Hashtable _typeMaps = new Hashtable();

        /// <summary>
        /// Set custom mapping for type deserializers
        /// </summary>
        /// <param name="type">Entity type to override</param>
        /// <param name="map">Mapping rules implementation, null to remove custom map</param>
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
        /// Gets type-map for the given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type to get a map for.</param>
        /// <returns>Type map implementation, DefaultTypeMap instance if no override present</returns>
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
        /// Gets type-map for the given type
        /// </summary>
        /// <returns>Type map instance, default is to create new instance of DefaultTypeMap</returns>
#pragma warning disable CA2211 // Non-constant fields should not be visible - I agree with you, but we can't do that until we break the API
        public ITypeMap GetTypeMapProvider(Type type) {
            return new DefaultTypeMap(type, this._client);
        } 
#pragma warning restore CA2211 // Non-constant fields should not be visible
    }
}
