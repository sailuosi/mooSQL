using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.clip
{
    /// <summary>
    /// 简化版的linq缓存，只对最简单的字段选择表达式进行转译并缓存
    /// </summary>
    internal class SimpleFieldExpCache
    {
        static FrequencyBasedCache<int, string> _cache;

        public static FrequencyBasedCache<int, string> Cache
        {
            get
            {

                if (_cache == null)
                {
                    _cache = new FrequencyBasedCache<int, string>(TimeSpan.FromMinutes(10));
                }
                return _cache;
            }
        }
    }
}
