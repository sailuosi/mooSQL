using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.linq;

namespace mooSQL.data
{
    public partial class SooRepository<T>
    {

        /// <summary>
        /// 获取一个SQL编织器
        /// </summary>
        /// <returns></returns>
        public SQLBuilder useSQL() { 
            return DBLive.useSQL();
        }
        /// <summary>
        /// 获取一个LINQ查询器
        /// </summary>
        /// <returns></returns>
        public IDbBus<T> useBus() {
            return DBLive.useDbBus<T>();
        }
        /// <summary>
        /// 获取批量SQL执行器
        /// </summary>
        /// <returns></returns>
        public BatchSQL useBatchSQL() { 
            return DBLive.useBatchSQL();
        }
        /// <summary>
        /// 获取本库另一个类的仓储
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        public SooRepository<R> ChangeTo<R>() where R : class , new()
        {
            return DBLive.useRepo<R>();
        }
        /// <summary>
        /// 获取一个SQL片段编织器
        /// </summary>
        /// <returns></returns>
        public SQLClip useClip()
        {
            return DBLive.useClip();
        }
        /// <summary>
        /// 获取一个SQL片段编织器，并自动from当前实体类
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public SQLClip useClip(out T t)
        {
            var c= DBLive.useClip();
            c.from<T>(out t);
            return c;
        }
        public R useClip<R>( Func<SQLClip, R> clipAction)
        {
            var clip = useClip();
            return clipAction(clip);
        }
        /// <summary>
        /// 获取一个SQL片段编织器，并自动from当前实体类
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="clipAction"></param>
        /// <returns></returns>
        public R useClip<R>(Func<SQLClip,T, R> clipAction)
        {
            var clip = useClip();
            clip.from<T>(out var t);
            return clipAction(clip,t);
        }
    }
}
