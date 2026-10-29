using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 方言工厂，依据方言类型 / 字符串键，产生方言（双通道）。
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
        /// <summary>
        /// 按枚举注册方言；同时以枚举名写入字符串通道（忽略大小写）。
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="creator"></param>
        void useDialect(DataBaseType dbType, Func<Dialect> creator);
        /// <summary>
        /// 按字符串键注册方言；若键可解析为 <see cref="DataBaseType"/> 则同步写入枚举通道。
        /// </summary>
        /// <param name="dialectKey"></param>
        /// <param name="creator"></param>
        void useDialect(string dialectKey, Func<Dialect> creator);
    }
}