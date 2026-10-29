using mooSQL.linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 基础方言工厂：枚举通道 + 字符串通道。
    /// </summary>
    public class DialectFactoryBase : IDialectFactory
    {
        /// <summary>
        /// 枚举 → 方言创建器
        /// </summary>
        protected readonly ConcurrentDictionary<DataBaseType, Func<Dialect>> dialectCreators =
            new ConcurrentDictionary<DataBaseType, Func<Dialect>>();

        /// <summary>
        /// 字符串键 → 方言创建器（忽略大小写）
        /// </summary>
        protected readonly ConcurrentDictionary<string, Func<Dialect>> dialectCreatorsByKey =
            new ConcurrentDictionary<string, Func<Dialect>>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public void useDialect(DataBaseType dbType, Func<Dialect> creator)
        {
            if (creator == null) throw new ArgumentNullException(nameof(creator));
            dialectCreators.TryAdd(dbType, creator);
            if (dbType != DataBaseType.None)
            {
                dialectCreatorsByKey.TryAdd(dbType.ToString(), creator);
            }
        }

        /// <inheritdoc/>
        public void useDialect(string dialectKey, Func<Dialect> creator)
        {
            if (string.IsNullOrWhiteSpace(dialectKey))
                throw new ArgumentException("方言键不能为空。", nameof(dialectKey));
            if (creator == null) throw new ArgumentNullException(nameof(creator));

            var key = dialectKey.Trim();
            dialectCreatorsByKey.TryAdd(key, creator);
            if (Enum.TryParse(key, true, out DataBaseType dbType) && dbType != DataBaseType.None)
            {
                dialectCreators.TryAdd(dbType, creator);
            }
        }

        /// <summary>
        /// 根据数据库配置加载方言（字符串键优先，再回落枚举）。
        /// </summary>
        protected virtual Dialect loadByDB(DataBase db)
        {
            Dialect res = null;
            Func<Dialect> creator = null;

            if (!string.IsNullOrWhiteSpace(db.dialectKey))
            {
                dialectCreatorsByKey.TryGetValue(db.dialectKey.Trim(), out creator);
            }

            if (creator == null && db.dbType != DataBaseType.None)
            {
                dialectCreators.TryGetValue(db.dbType, out creator);
            }

            if (creator == null && db.dbType != DataBaseType.None)
            {
                dialectCreatorsByKey.TryGetValue(db.dbType.ToString(), out creator);
            }

            if (creator != null)
            {
                res = creator();
            }

            if (res == null)
            {
                return null;
            }

            if (res.clauseTranslator == null)
            {
                res.clauseTranslator = new ClauseTranslateVisitor(res);
            }
            if (res.db == null)
            {
                res.db = db;
            }

            return res;
        }

        /// <inheritdoc/>
        public virtual Dialect getDialect(DataBase db)
        {
            var res = loadByDB(db);
            if (res == null)
            {
                var hint = !string.IsNullOrWhiteSpace(db?.dialectKey)
                    ? db.dialectKey
                    : db?.dbType.ToString();
                throw new NotSupportedException(
                    "尚未支持当前数据库连接！请检查数据库类型、dialectKey，或通过 Provider 包 UseXxx / useDialect 注册方言。标识=" + hint);
            }
            return res;
        }

        /// <inheritdoc/>
        public virtual Dictionary<int, DataBase> loadDBConfig(DBInsCash dBIns)
        {
            return new Dictionary<int, DataBase>();
        }
    }
}
