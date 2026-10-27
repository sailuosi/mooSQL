using System.Reflection;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;

namespace LiteOrm.Benchmark
{
    /// <summary>
    /// mooSQL 基准连接：与其它 ORM 共用同一 MySQL/SQLite/Oracle 连接串。
    /// </summary>
    public static class MooSqlDb
    {
        private static readonly object _lock = new object();
        private static DBInstance? _db;
        private static string? _cs;
        private static string? _provider;

        public static DBInstance Db
        {
            get
            {
                if (_db == null)
                    throw new InvalidOperationException("MooSqlDb.EnsureInit must be called from GlobalSetup.");
                return _db;
            }
        }

        public static void EnsureInit(string connectionString, string provider)
        {
            if (_db != null
                && string.Equals(_cs, connectionString, StringComparison.Ordinal)
                && string.Equals(_provider, provider, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lock (_lock)
            {
                if (_db != null
                    && string.Equals(_cs, connectionString, StringComparison.Ordinal)
                    && string.Equals(_provider, provider, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var cash = new DBClientBuilder()
                    .useEntityAnalyser(new BenchmarkEntityAnalyser())
                    .doBuild();

                cash.addDataBase(0, new DataBase
                {
                    dbType = MapDbType(provider),
                    DBConnectStr = connectionString,
                    name = "0"
                });

                _db = cash.getInstance(0);
                _cs = connectionString;
                _provider = provider;
            }
        }

        private static DataBaseType MapDbType(string provider)
        {
            var p = (provider ?? "").ToLowerInvariant();
            if (p.Contains("sqlite")) return DataBaseType.SQLite;
            if (p.Contains("oracle")) return DataBaseType.Oracle;
            return DataBaseType.MySQL;
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
                    result.DbTableName = Entity.Name;

                if (result.DType == DBTableType.None)
                    result.DType = DBTableType.Table;

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
                    };
                }

                if (propertyInfo.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    col.IsPrimarykey = true;
                    col.IsIdentity = true;
                }

                // 导航属性不入表
                if (propertyInfo.PropertyType == typeof(BenchmarkUser)
                    || propertyInfo.PropertyType == typeof(BenchmarkLog)
                    || propertyInfo.PropertyType == typeof(BenchmarkLogView))
                {
                    col.IsIgnore = true;
                }

                return col;
            }
        }
    }
}
