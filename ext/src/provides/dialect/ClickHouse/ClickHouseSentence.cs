#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// ClickHouse 元数据：system.tables / system.columns / system.databases。
    /// </summary>
    public class ClickHouseSentence : SQLSentence
    {
        /// <summary>
        /// ClickHouseSentence type.
        /// </summary>
        /// <summary>
        /// ClickHouseSentence type.
        /// </summary>
        public ClickHouseSentence(Dialect dia) : base(dia) { }
/// <summary>
/// Loads column metadata for the specified table.
/// </summary>
 

        public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
        {
            builder.from("t", t =>
            {
                t.select("name as columnName, type as columnType, 0 as columnLen, comment as columnDesc ")
                    .from("system.columns")
                    .where("table", tableName)
                    .where("database = currentDatabase()");
            });
            return builder;
        }
/// <summary>
/// SQL used to list databases.
/// </summary>
 

        public override string GetDataBaseSql => "SELECT name AS Name FROM system.databases ORDER BY name";
/// <summary>
/// SQL used to list column info for a table.
/// </summary>
 

        public override string GetColumnInfosByTableNameSql =>
            "SELECT name AS Name, type AS DataType, " +
            "0 AS ColumnLength, 0 AS Scale, " +
            "default_expression AS DefaultValue, comment AS Comment, " +
            "false AS IsPrimary, false AS IsIdentity, " +
            "true AS IsNullable " +
            "FROM system.columns WHERE table = '{0}' AND database = currentDatabase() ORDER BY position";
/// <summary>
/// Loads column captions for the specified table.
/// </summary>
 

        public override List<DbColumnCaption> GetDbColumnCaptionsByTableName(string tableName)
        {
            return DBLive.useSQL()
                .select("name AS Name, comment AS Caption")
                .from("system.columns")
                .where("table", tableName)
                .where("database = currentDatabase()")
                .query<DbColumnCaption>()
                .ToList();
        }
/// <summary>
/// SQL used to list tables.
/// </summary>
 

        public override string GetTableInfoListSql =>
            "SELECT name AS Name, comment AS Comment FROM system.tables WHERE database = currentDatabase() AND engine NOT LIKE '%View%'";
/// <summary>
/// SQL used to list views.
/// </summary>
 

        public override string GetViewInfoListSql =>
            "SELECT name AS Name, comment AS Description FROM system.tables WHERE database = currentDatabase() AND engine LIKE '%View%'";
/// <summary>
/// Loads all tables matching the given options.
/// </summary>
 

        public override List<DbTableInfo> GetDbTableListAll(GetDbTableListOptions options = null)
        {
            if (options != null && options.CurrentDatabaseOnly && string.IsNullOrEmpty(options.CurrentDatabaseName))
                options.CurrentDatabaseName = GetCurrentDatabaseName();

            var kit = DBLive.useSQL()
                .select("database AS Schema, name AS Name, comment AS Comment, total_rows AS RowCount")
                .from("system.tables")
                .where("engine NOT LIKE '%View%'")
                .where("database NOT IN ('system','INFORMATION_SCHEMA','information_schema')");

            options?.ApplyToBuilder(kit, new DbTableListSqlColumns
            {
                Name = "name",
                Comment = "comment",
                RowCount = "total_rows",
                Schema = "database",
                Database = "database",
                DefaultOrderBy = "database, name",
            });
            if (options == null)
                kit.orderBy("database, name");

            return FinalizeDbTableListAll(kit.query<DbTableInfo>().ToList(), options);
        }
/// <summary>
/// Returns the current database name.
/// </summary>
 

        protected override string GetCurrentDatabaseName()
        {
            try { return DBLive.ExeQueryScalar<string>("SELECT currentDatabase()", null); }
            catch { return null; }
        }

        /// <summary>ClickHouse 库即命名空间；返回 system.databases。</summary>
        public override List<DbSchemaInfo> GetDbSchemaList(GetDbSchemaListOptions options = null)
        {
            EnsureSchemaListCurrentDatabase(options);
            var current = GetCurrentDatabaseName();
            var kit = DBLive.useSQL()
                .select("name as Name, name as Database")
                .from("system.databases")
                .where("name NOT IN ('system','INFORMATION_SCHEMA','information_schema')");
            if (options != null && options.HasDatabaseScope && !string.IsNullOrEmpty(options.EffectiveDatabaseName))
                kit.where("name", options.EffectiveDatabaseName);
            kit.orderBy("name");
            var list = kit.query<DbSchemaInfo>().ToList();
            foreach (var s in list)
                s.IsDefault = !string.IsNullOrEmpty(current)
                    && string.Equals(s.Name, current, StringComparison.OrdinalIgnoreCase);
            return list;
        }
/// <summary>
/// Builds SQL that checks whether a table exists.
/// </summary>
 

        public override SQLCmd buildHasTable(string TableName)
        {
            return DBLive.useSQL()
                .select("count()")
                .from("system.tables")
                .where("database = currentDatabase()")
                .where("name", TableName)
                .toSelect();
        }
/// <summary>
/// Returns the primary-key name for the specified table.
/// </summary>
 

        public override string GetTablePKName(string table) => string.Empty;
/// <summary>
/// Returns whether the specified object is a view.
/// </summary>
 

        public override bool? IsView(string tabelOrViewName, string dbName = null)
        {
            if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
            var engine = DBLive.useSQL()
                .select("engine")
                .from("system.tables")
                .where("database = currentDatabase()")
                .where("name", tabelOrViewName)
                .queryRowString(null);
            if (string.IsNullOrEmpty(engine)) return false;
            return engine.IndexOf("View", StringComparison.OrdinalIgnoreCase) >= 0;
        }
/// <summary>
/// Returns whether the specified column exists.
/// </summary>
 

        public override bool? IsExitsTableCol(string table, string col)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
            var c = DBLive.useSQL()
                .from("system.columns")
                .where("database = currentDatabase()")
                .where("table", table)
                .where("name", col)
                .count();
            return c > 0;
        }
/// <summary>
/// Returns whether the specified index exists.
/// </summary>
 

        public override bool IsExitsTableIndex(string table, string indexName)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
            try
            {
                var c = DBLive.useSQL()
                    .from("system.data_skipping_indices")
                    .where("database = currentDatabase()")
                    .where("table", table)
                    .where("name", indexName)
                    .count();
                return c > 0;
            }
            catch
            {
                return false;
            }
        }
/// <summary>
/// Returns whether the exception indicates a lost connection.
/// </summary>
 

        public override bool IsConnectionLost(Exception ex)
        {
            if (ex == null) return false;
            return MatchMessage(ex,
                "Connection refused",
                "actively refused",
                "Unable to connect",
                "The request was canceled",
                "Error while reading");
        }
    }
}
#endif
