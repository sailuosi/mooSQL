// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    public static class BaseReadingExtensions
    {
        public static void AddNotNull<K, T>(this Dictionary<K, T> map, K key, T value)
        {
            if (value == null) { return; }
            if (map.ContainsKey(key))
            {
                map[key] = value;
            }
            else
            {
                map.Add(key, value);
            }
        }
    }
}