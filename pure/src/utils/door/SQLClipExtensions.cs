using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// SQLClip内置扩展
    /// </summary>
    public static class SQLClipBuildInExtensions
    {
        /// <summary>
        /// 查询并范围列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static List<T> queryToList<T>(this SQLClip<T> clip)
        {
            return clip.queryList().ToList();
        }
    }
}
