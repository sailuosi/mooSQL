using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// BatchSQL的扩展方法
    /// </summary>
    public static class BatchSQLExtentions
    {
        /// <summary>
        /// 修改一个数据层实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static BatchSQL modifyBy<T>(this BatchSQL builder, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.DBLive.useClip();
            clip.setTable<T>(out var t);
            doClipFilting(clip, t);
            var cmd= clip.toUpdate();
            builder.addSQL(cmd);
            return builder;
        }

        /// <summary>
        /// 快速删除实体，按照自定义条件，需要手写where部分。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="doClipFilting"></param>
        /// <returns></returns>
        public static BatchSQL removeBy<T>(this BatchSQL builder, Action<SQLClip, T> doClipFilting) where T : class, new()
        {
            var clip = builder.DBLive.useClip();
            clip.setTable<T>(out var t);
            doClipFilting(clip, t);
            var cmd= clip.toDelete();
            builder.addSQL(cmd);
            return builder;
        }
    }
}
