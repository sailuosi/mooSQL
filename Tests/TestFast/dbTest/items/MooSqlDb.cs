using System;
using System.Reflection;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;

namespace dbTest.items
{
    /// <summary>
    /// mooSQL 共享 SQLite 连接（与其它 ORM 使用同一 ITest.sqlLiteDb）。
    /// </summary>
    public static class MooSqlDb
    {
        private static readonly object _lock = new object();
        private static DBInstance _db;

        public static DBInstance Db
        {
            get
            {
                EnsureInit();
                return _db;
            }
        }

        public static void EnsureInit()
        {
            if (_db != null)
            {
                return;
            }

            lock (_lock)
            {
                if (_db != null)
                {
                    return;
                }

                // 复测：开启执行模板缓存（HashCache 忙等已修，观察 Allocated 是否回到正常）。
                SQLBuilder.DefaultUseScriptTemplateCache = true;

                var cash = new DBClientBuilder()
                    .useEntityAnalyser(new BenchmarkEntityAnalyser())
                    .doBuild();

                cash.addDataBase(0, new DataBase
                {
                    dbType = DataBaseType.SQLite,
                    DBConnectStr = ITest.sqlLiteDb,
                    name = "0"
                });

                _db = cash.getInstance(0);
            }
        }

        /// <summary>
        /// 按类名/属性名约定映射，兼容无 SooTable/SooColumn 的基准实体。
        /// </summary>
        private sealed class BenchmarkEntityAnalyser : MooEntityAnalyser
        {
            public override bool CanParse(Type type) => true;

            protected override EntityInfo AfterReadEntityAttr(Type Entity, EntityInfo result)
            {
                if (string.IsNullOrWhiteSpace(result.DbTableName))
                {
                    result.DbTableName = Entity.Name;
                }

                if (result.DType == DBTableType.None)
                {
                    result.DType = DBTableType.Table;
                }

                return result;
            }

            public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, EntityInfo entityInfo, EntityColumn entityColumn)
            {
                var col = base.ParseColumn(entity, propertyInfo, entityInfo, entityColumn);
                if (col == null)
                {
                    col = new EntityColumn(entityInfo)
                    {
                        PropertyInfo = propertyInfo,
                        PropertyName = propertyInfo.Name,
                        DbColumnName = propertyInfo.Name,
                        EntityName = entityInfo.EntityName,
                        Kind = FieldKind.Base,
                        IsPrimarykey = propertyInfo.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
                    };
                }

                return col;
            }
        }
    }
}
