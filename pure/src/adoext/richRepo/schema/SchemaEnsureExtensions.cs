using System;
using System.Collections.Generic;
using mooSQL.data;
using mooSQL.data.richRepo.schema;

namespace mooSQL.data.richRepo.schema
{
    /// <summary>
    /// DDL / DBInstance Schema 扩展。
    /// </summary>
    public static class SchemaEnsureExtensions
    {
        /// <summary>一键对齐实体表结构。</summary>
        public static SchemaEnsureResult ensure<T>(this DDLBuilder ddl, SyncMode mode = SyncMode.AddMissingColumns)
        {
            if (ddl == null) throw new ArgumentNullException(nameof(ddl));
            return SchemaEnsure.Ensure<T>(ddl.DBLive, new SchemaEnsureOptions { Mode = mode });
        }

        /// <summary>一键对齐（完整选项）。</summary>
        public static SchemaEnsureResult ensure<T>(this DDLBuilder ddl, SchemaEnsureOptions options)
        {
            if (ddl == null) throw new ArgumentNullException(nameof(ddl));
            return SchemaEnsure.Ensure<T>(ddl.DBLive, options);
        }

        /// <summary>DBInstance 快捷 Ensure。</summary>
        public static SchemaEnsureResult ensureSchema<T>(this DBInstance db, SyncMode mode = SyncMode.AddMissingColumns)
            => SchemaEnsure.Ensure<T>(db, new SchemaEnsureOptions { Mode = mode });

        /// <summary>DBInstance 快捷 Ensure（选项）。</summary>
        public static SchemaEnsureResult ensureSchema<T>(this DBInstance db, SchemaEnsureOptions options)
            => SchemaEnsure.Ensure<T>(db, options);
    }
}
