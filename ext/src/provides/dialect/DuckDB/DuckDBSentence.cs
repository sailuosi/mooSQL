#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB 元数据 / 探活 SQL（information_schema + duckdb_*）。
    /// </summary>
    public class DuckDBSentence : SQLSentence
    {
        public DuckDBSentence(Dialect dia) : base(dia) { }

        public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
        {
            builder.from("t", t =>
            {
                t.select("co.column_name as columnName, co.data_type as columnType, co.character_maximum_length as columnLen, '' as columnDesc ")
                    .from("information_schema.columns co")
                    .where("table_name", tableName);
            });
            return builder;
        }

        public override string GetDataBaseSql => "SELECT current_database() AS Name";

        public override string GetColumnInfosByTableNameSql =>
            "SELECT column_name AS Name, data_type AS DataType, " +
            "character_maximum_length AS ColumnLength, numeric_scale AS Scale, " +
            "column_default AS DefaultValue, '' AS Comment, " +
            "false AS IsPrimary, " +
            "CASE WHEN column_default LIKE '%nextval%' OR column_default LIKE '%identity%' THEN true ELSE false END AS IsIdentity, " +
            "CASE WHEN is_nullable = 'YES' THEN true ELSE false END AS IsNullable " +
            "FROM information_schema.columns WHERE table_name = '{0}' ORDER BY ordinal_position";

        public override List<DbColumnCaption> GetDbColumnCaptionsByTableName(string tableName)
        {
            return DBLive.useSQL()
                .select("column_name AS Name, '' AS Caption")
                .from("information_schema.columns")
                .where("table_name", tableName)
                .query<DbColumnCaption>()
                .ToList();
        }

        public override string GetTableInfoListSql =>
            "SELECT table_name AS Name, '' AS Comment FROM information_schema.tables WHERE table_schema = 'main' AND table_type = 'BASE TABLE'";

        public override string GetViewInfoListSql =>
            "SELECT table_name AS Name, '' AS Description FROM information_schema.tables WHERE table_schema = 'main' AND table_type = 'VIEW'";

        public override SQLCmd buildHasTable(string TableName)
        {
            return DBLive.useSQL()
                .select("count(1)")
                .from("information_schema.tables")
                .where("table_schema", "main")
                .where("table_name", TableName)
                .toSelect();
        }

        public override string GetTablePKName(string table) => string.Empty;

        public override bool? IsView(string tabelOrViewName, string dbName = null)
        {
            if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
            var type = DBLive.useSQL()
                .select("table_type")
                .from("information_schema.tables")
                .where("table_schema", "main")
                .where("table_name", tabelOrViewName)
                .queryRowString(null);
            if (string.IsNullOrEmpty(type)) return false;
            return string.Equals(type, "VIEW", StringComparison.OrdinalIgnoreCase);
        }

        public override bool? IsExitsTableCol(string table, string col)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
            var c = DBLive.useSQL()
                .from("information_schema.columns")
                .where("table_schema", "main")
                .where("table_name", table)
                .where("column_name", col)
                .count();
            return c > 0;
        }

        public override bool IsExitsTableIndex(string table, string indexName)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
            try
            {
                var c = DBLive.useSQL()
                    .from("duckdb_indexes()")
                    .where("table_name", table)
                    .where("index_name", indexName)
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
                "Connection was closed",
                "database is already closed",
                "IO Error",
                "Could not set lock on file");
        }

        /// <summary>
        /// 生成 COPY 导出 SQL（CSV/Parquet 等）；调用方自行执行。
        /// </summary>
        public string BuildCopyToSql(string tableOrQuery, string filePath, string format = "CSV")
            => $"COPY ({tableOrQuery}) TO '{filePath.Replace("'", "''")}' (FORMAT {format})";

        /// <summary>
        /// 生成 COPY 导入 SQL；调用方自行执行。
        /// </summary>
        public string BuildCopyFromSql(string tableName, string filePath, string format = "CSV")
            => $"COPY {tableName} FROM '{filePath.Replace("'", "''")}' (FORMAT {format})";
    }
}
#endif
