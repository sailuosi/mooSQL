using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 字段的where扩展
    /// </summary>
    public static class WhereFieldLINQExtensions
    {
        /// <summary>
        /// 模糊查询匹配（CLR 语义等同 <see cref="string.Contains(string)"/>；SQL 编译时自动在模式两侧添加通配符，生成 <c>LIKE '%value%'</c>）。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="tar"></param>
        /// <returns></returns>
        public static bool Like(this string src, string tar) { 
            return src.Contains(tar);
        }
        /// <summary>
        /// 左前缀匹配（CLR 语义等同 <see cref="string.StartsWith(string)"/>；SQL 编译时自动在模式末尾添加通配符，生成 <c>LIKE 'value%'</c>）。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="tar"></param>
        /// <returns></returns>
        public static bool LikeLeft(this string src, string tar)
        {
            return src.StartsWith(tar);
        }
        /// <summary>
        /// 列表包含
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="tar"></param>
        /// <returns></returns>
        public static bool InList<T>(this T src, IEnumerable<T> tar) { 
            return tar.Contains(src);
        }
        /// <summary>
        /// 判空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        public static bool IsNull<T>(this T src)
        {
            return src==null;
        }
        /// <summary>
        /// 扩展版
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string src)
        {
            return string.IsNullOrWhiteSpace(src);
        }
        /// <summary>
        /// 非空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        public static bool IsNotNull<T>(this T src)
        {
            return src != null;
        }
    }
}
