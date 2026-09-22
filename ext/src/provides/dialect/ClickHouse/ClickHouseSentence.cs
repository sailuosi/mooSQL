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
        public ClickHouseSentence(Dialect dia) : base(dia) { }

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

        public override string GetDataBaseSql => "SELECT name AS Name FROM system.databases ORDER BY name";

        public override string GetColumnInfosByTableNameSql =>
            "SELECT name AS Name, type AS DataType, " +
            "0 AS ColumnLength, 0 AS Scale, " +
            "default_expression AS DefaultValue, comment AS Comment, " +
            "false AS IsPrimary, false AS IsIdentity, " +
            "true AS IsNullable " +
            "FROM system.columns WHERE table = '{0}' AND database = currentDatabase() ORDER BY position";

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

        public override string GetTableInfoListSql =>
            "SELECT name AS Name, comment AS Comment FROM system.tables WHERE database = currentDatabase() AND engine NOT LIKE '%View%'";

        public override string GetViewInfoListSql =>
            "SELECT name AS Name, comment AS Description FROM system.tables WHERE database = currentDatabase() AND engine LIKE '%View%'";

        public override SQLCmd buildHasTable(string TableName)
        {
            return DBLive.useSQL()
                .select("count()")
                .from("system.tables")
                .where("database = currentDatabase()")
                .where("name", TableName)
                .toSelect();
        }

        public override string GetTablePKName(string table) => string.Empty;

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
