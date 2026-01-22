using mooSQL.linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 基础方言工厂
    /// </summary>
    public class DialectFactoryBase : IDialectFactory
    {
        /// <summary>
        /// 方言字典
        /// </summary>
        protected readonly ConcurrentDictionary<DataBaseType, Func<Dialect>> dialectCreators= new ConcurrentDictionary<DataBaseType, Func<Dialect>>();
        /// <summary>
        /// 注册方言
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="creator"></param>
        public void useDialect(DataBaseType dbType, Func<Dialect> creator)
        {
            dialectCreators.TryAdd(dbType, creator);
        }
        /// <summary>
        /// 根据数据库类型加载
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        protected virtual Dialect loadByDB(DataBase db) {
            Dialect res = null;
            //实际的场景可能有许多意外，主要是按数据库类型加载，但实际可能会按数据库版本加载，甚至按业务版本加载
            if (dialectCreators.TryGetValue(db.dbType, out var creator))
            {
                res = creator();
            }
            if (res.clauseTranslator == null) {
                res.clauseTranslator = new ClauseTranslateVisitor(res);
            }
            if (res.db == null) {
                res.db = db;
            }
            
            return res;
        }
        /// <summary>
        /// 获取方言，结果为可用的方言
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public virtual Dialect getDialect(DataBase db)
        {
            var res = loadByDB(db);
            if (res == null) {
                throw new NotSupportedException("尚未支持当前数据库连接！请检查数据库类型或者自定义方言！");
            }
            return res;
        }
        /// <summary>
        /// 未实现
        /// </summary>
        /// <param name="dBIns"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual Dictionary<int, DataBase> loadDBConfig(DBInsCash dBIns)
        {
            return new Dictionary<int, DataBase>();
        }
    }
}
