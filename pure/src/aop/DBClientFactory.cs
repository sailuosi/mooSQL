using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 客户侧工厂类，用于创建各类客户端对象。定义此类，便于业务系统侧扩展功能以自定义行为。
    /// </summary>
    public class DBClientFactory
    {
        /// <summary>
        /// 创建SQL构建器对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual SQLBuilder useSQL(DBInstance DB) { 
            var tar= new SQLBuilder();
            tar.setDBInstance(DB);
            return tar;
        }
        /// <summary>
        /// 创建批量SQL构建器对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual BatchSQL useBatchSQL( DBInstance DB)
        {
            var kit = new BatchSQL(DB);
            return kit;
        }
        /// <summary>
        /// 创建DBRunner对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual DBRunner useDBRunner(DBInstance DB)
        {
            var kit = new DBRunner(DB);
            return kit;
        }
        /// <summary>
        /// 创建SooRepository对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual SooRepository<T> useRepo<T>(DBInstance DB) where T : class, new()
        {
            var t = new SooRepository<T>(DB);
            return t;
        }
        /// <summary>
        /// 创建SooUnitOfWork对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual SooUnitOfWork useWork(DBInstance DB)
        {
            var t = new SooUnitOfWork(DB);
            return t;
        }
        /// <summary>
        /// 创建DDLBuilder对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual DDLBuilder useDDL(DBInstance DB)
        {
            var t = new DDLBuilder(DB);
            return t;
        }
        /// <summary>
        /// 创建SQLClip对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual SQLClip useClip(DBInstance DB,SQLBuilder kit)
        {
            var t = new SQLClip(DB,kit);
            return t;
        }
        /// <summary>
        /// 创建BulkBase对象。
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public virtual BulkBase useBulk(DBInstance DB)
        {
            var t = new BulkBase();
            t.DBLive = DB;
            return t;
        }

        public virtual EntityTranslator getEntityTranslator() { 
            return new EntityTranslator();
        }
    }
}
