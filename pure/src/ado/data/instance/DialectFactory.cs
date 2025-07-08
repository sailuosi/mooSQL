





using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 方言工厂，依据方言类型，产生方言。
    /// </summary>
    public interface IDialectFactory {
        /// <summary>
        /// 取得方言
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        Dialect getDialect(DataBase db);
        /// <summary>
        /// 加载数据库配置
        /// </summary>
        /// <param name="dBIns"></param>
        /// <returns></returns>
        Dictionary<int, DataBase> loadDBConfig(DBInsCash dBIns);


    }
}