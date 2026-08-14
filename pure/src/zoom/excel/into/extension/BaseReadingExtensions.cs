// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    /// <summary>
    /// Excel 读取与导入流程中使用的字典等扩展方法。
    /// </summary>
    public static class BaseReadingExtensions
    {
        /// <summary>
        /// 若 <paramref name="value"/> 非空则写入字典；已存在键时覆盖为新值。
        /// </summary>
        /// <typeparam name="K">键类型。</typeparam>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="map">目标字典。</param>
        /// <param name="key">键。</param>
        /// <param name="value">值；为 null 时不修改字典。</param>
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