
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 查询工具扩展
    /// </summary>
    public static class DBQueryableExtension
    {
        private static DbContextReadyBook<DBInstance> _book;
        private static DbContextReadyBook<DBInstance> Book
        {
            get {
                if (_book == null) { 
                    _book = new DbContextReadyBook<DBInstance>();
                }
                return _book;
            }
        }
        /// <summary>
        /// 开启查询表达式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static DbBus<T> useDbBus<T>(this DBInstance DB)
        {

            var cont = Book.Get(DB);
            if (cont != null)
            {
                return new EnDbBus<T>(cont, typeof(T), cont.Factory);
            }

            var connect = new DbContext();
            var fac = new FastLinqFactory();
            connect.Factory = fac;
            connect.DB = DB;
            Book.Add(DB, connect);
            return new EnDbBus<T>(connect, typeof(T), fac);
        }
        /// <summary>
        /// 创建一个SQLBuilder
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static SQLBuilder useSQL(this DBInstance DB)
        {
            return DB.client.ClientFactory.useSQL(DB);
        }
        /// <summary>
        /// 批量SQL执行器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static BatchSQL useBatchSQL(this DBInstance DB)
        {
            return DB.client.ClientFactory.useBatchSQL(DB);
        }
        /// <summary>
        /// 获取一个执行器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static DBRunner useDBRunner(this DBInstance DB)
        {
            return DB.client.ClientFactory.useDBRunner(DB);
        }
        /// <summary>
        /// 获取一个仓储实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static SooRepository<T> useRepo<T>(this DBInstance DB) where T : class, new() 
        {
            return DB.client.ClientFactory.useRepo<T>(DB);
        }
        /// <summary>
        /// 获取一个工作单元
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static SooUnitOfWork useWork(this DBInstance DB) {
            return DB.client.ClientFactory.useWork(DB);
        }
        /// <summary>
        /// 获取一个工作单元（别名）
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static SooUnitOfWork useUnitOfWork(this DBInstance DB)
        {
            return DB.client.ClientFactory.useWork(DB);
        }
        
        /// <summary>
        /// 数据库结构的访问器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static DDLBuilder useDDL(this DBInstance DB)
        {
            return DB.client.ClientFactory.useDDL(DB);
        }
        /// <summary>
        /// 创建一个支持实体解析的SQL片段构建器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static SQLClip useClip(this DBInstance DB,SQLBuilder kit=null)
        {
            return DB.client.ClientFactory.useClip(DB, kit);
        }
        /// <summary>
        /// 批量插入器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public static BulkBase useBulk(this DBInstance DB)
        {
            return DB.client.ClientFactory.useBulk(DB);
        }
    }
}
