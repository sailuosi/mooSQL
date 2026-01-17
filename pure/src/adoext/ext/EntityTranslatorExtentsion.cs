using mooSQL.data.context;
using mooSQL.data.linq;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.clip
{
    /// <summary>
    /// 实体转义扩展
    /// </summary>
    public static class EntityTranslatorExtentsion
    {
        /// <summary>
        /// 转移表达式的字段名称查找，使用缓存提高性能
        /// </summary>
        /// <param name="DB"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static string FindFieldName(this DBInstance DB, Expression keySelector,bool withCallerNick=false)
        {
            var f= FindField(DB, keySelector);
            if (f == null)
            {
                return null;
            }
            return f.ToSQLField(withCallerNick,DB);
        }
        /// <summary>
        /// 获取字段对象，使用缓存提高性能
        /// </summary>
        /// <param name="DB"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static FastBusField FindField(this DBInstance DB, Expression keySelector)
        {
            var parser = new ClipExpSameCheckor();
            var hashcode = parser.GetHashCode(keySelector);

            if (FastLinqParseCache.Cache.TryGetValue(hashcode, out var target))
            {
                return target;
            }
            else
            {
                var tar = EntityTranslator.FindFieldName(DB, keySelector);
                if (tar == null) {
                    return null;
                }
                FastLinqParseCache.Cache.Add(hashcode, tar);
                return tar;
            }
        }
    }



    internal class FastLinqParseCache
    {

        static FrequencyBasedCache<int, FastBusField> _cache;

        public static FrequencyBasedCache<int, FastBusField> Cache
        {
            get
            {

                if (_cache == null)
                {
                    _cache = new FrequencyBasedCache<int, FastBusField>(TimeSpan.FromMinutes(10));
                }
                return _cache;
            }
        }


    }
}
